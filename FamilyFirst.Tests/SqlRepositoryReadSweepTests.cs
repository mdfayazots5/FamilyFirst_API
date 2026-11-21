using System.Reflection;
using FamilyFirst.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyFirst.Tests;

/// <summary>
/// Calls every read-only repository method (Get*/List*/Exists*/Count*/Search*/Is*/Has*) against the scripted
/// SQL schema with placeholder arguments. Any EF translation failure or invalid column is a runtime 500 in
/// waiting (ProjectOverview §23). Business exceptions and empty results are fine — only SQL/EF contract errors fail.
/// </summary>
public sealed class SqlRepositoryReadSweepTests
{
    private static readonly string[] ReadPrefixes = { "Get", "List", "Exists", "Count", "Search", "Is", "Has", "Find" };

    [SqlServerFact]
    public async Task Every_read_only_repository_method_translates_and_matches_the_schema()
    {
        var repositoryTypes = typeof(FamilyFirstDbContext).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Repository", StringComparison.Ordinal)
                && t.GetConstructors().Any(c => c.GetParameters() is [{ ParameterType: var p }] && p == typeof(FamilyFirstDbContext)))
            .ToArray();
        repositoryTypes.Should().NotBeEmpty();

        var failures = new List<string>();
        var invoked = 0;
        foreach (var type in repositoryTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(m => ReadPrefixes.Any(p => m.Name.StartsWith(p, StringComparison.Ordinal))
                                     && typeof(Task).IsAssignableFrom(m.ReturnType) && !m.IsGenericMethodDefinition))
            {
                if (!TryBuildArguments(method, out var args)) continue;
                await using var db = new FamilyFirstDbContext(
                    new DbContextOptionsBuilder<FamilyFirstDbContext>().UseSqlServer(SqlTestDatabase.ConnectionString()).Options);
                var repository = Activator.CreateInstance(type, db)!;
                invoked++;
                try
                {
                    await (Task)method.Invoke(repository, args)!;
                }
                catch (Exception ex) when (IsContractFailure(ex, out var reason))
                {
                    failures.Add($"{type.Name}.{method.Name}: {reason}");
                }
                catch
                {
                    // Business/argument exceptions with placeholder data are not contract failures.
                }
            }
        }

        invoked.Should().BeGreaterThan(50, "the sweep must actually exercise the repositories");
        failures.Should().BeEmpty();
    }

    private static bool IsContractFailure(Exception ex, out string reason)
    {
        var inner = ex is TargetInvocationException { InnerException: { } i } ? i : ex;
        reason = inner.Message.Split('\n')[0];
        return inner switch
        {
            InvalidOperationException ioe when ioe.Message.Contains("could not be translated", StringComparison.Ordinal) => true,
            SqlException sql when sql.Number is 207 or 208 or 4104 => true, // invalid column / object / multi-part id
            _ => false
        };
    }

    private static bool TryBuildArguments(MethodInfo method, out object?[] args)
    {
        var parameters = method.GetParameters();
        args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var type = Nullable.GetUnderlyingType(parameters[i].ParameterType) ?? parameters[i].ParameterType;
            var name = parameters[i].Name ?? string.Empty;
            object? value;
            if (type == typeof(CancellationToken)) value = CancellationToken.None;
            else if (type == typeof(Guid)) value = Guid.NewGuid();
            else if (type == typeof(string)) value = "x";
            else if (type == typeof(int)) value = name.Contains("size", StringComparison.OrdinalIgnoreCase) ? 10 : 1;
            else if (type == typeof(long)) value = 1L;
            else if (type == typeof(bool)) value = false;
            else if (type == typeof(decimal)) value = 1m;
            else if (type == typeof(double)) value = 1d;
            else if (type == typeof(DateTime)) value = DateTime.UtcNow;
            else if (type == typeof(DateOnly)) value = DateOnly.FromDateTime(DateTime.UtcNow);
            else if (type == typeof(TimeOnly)) value = TimeOnly.FromDateTime(DateTime.UtcNow);
            else if (type.IsEnum) value = Enum.GetValues(type).GetValue(0);
            else if (type == typeof(Guid[]) || type == typeof(IReadOnlyCollection<Guid>) || type == typeof(IEnumerable<Guid>))
                value = new[] { Guid.NewGuid() };
            else if (type == typeof(long[]) || type == typeof(IReadOnlyCollection<long>) || type == typeof(IEnumerable<long>))
                value = new[] { 1L };
            else if (parameters[i].ParameterType.IsClass || Nullable.GetUnderlyingType(parameters[i].ParameterType) is not null)
                value = null; // optional filters / DTOs — skip methods that need a non-null object below
            else return false;

            if (value is null && parameters[i].ParameterType.IsClass && type != typeof(string)
                && !IsNullableReference(parameters[i]))
                return false;
            args[i] = value;
        }
        return true;
    }

    private static bool IsNullableReference(ParameterInfo parameter) =>
        new NullabilityInfoContext().Create(parameter).WriteState == NullabilityState.Nullable;
}
