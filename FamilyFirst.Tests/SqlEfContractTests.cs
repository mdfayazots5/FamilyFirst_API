using FamilyFirst.Infrastructure.Data;
using FamilyFirst.Infrastructure.Data.Repositories.Implementations;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FamilyFirst.Tests;

/// <summary>
/// Guards the EF model against DB contract drift (ProjectOverview §23): every mapped column must exist in
/// the scripted schema, and repository queries must translate to SQL (no computed/alias properties in LINQ).
/// </summary>
public sealed class SqlEfContractTests
{
    [SqlServerFact]
    public async Task Every_mapped_ef_column_exists_in_the_scripted_schema()
    {
        await using var db = CreateContext();
        var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var connection = new SqlConnection(SqlTestDatabase.ConnectionString()))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TABLE_NAME + '.' + COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo'";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) actual.Add(reader.GetString(0));
        }

        var missing = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue;
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(store);
                if (column is not null && !actual.Contains($"{table}.{column}"))
                    missing.Add($"{entity.ClrType.Name}.{property.Name} -> {table}.{column}");
            }
        }

        missing.Should().BeEmpty("every EF-mapped property needs a real column; alias properties must be Ignore()d");
    }

    [SqlServerFact]
    public async Task Repository_queries_using_entity_aliases_translate_to_sql()
    {
        var unknown = Guid.NewGuid();
        await using var db = CreateContext();

        await new CommentTemplateRepository(db).GetByIdAsync(unknown, default);
        await new AdminRepository(db).GetDashboardAsync(default);
        await new AdminRepository(db).ListPlansAsync(default);
        await new AdminRepository(db).GetPlanEntityByIdAsync(1, default);
        await new FamilyRepository(db).GetPlanByIdAsync(1, default);
        await new AttendanceRecordRepository(db).ListBySessionAsync(unknown, default);
        await new FamilyAdminConfigRepository(db).GetNotificationRuleByIdAsync(unknown, unknown, default);
        await new FamilyAdminConfigRepository(db).GetCustomAttendanceStatusByIdAsync(unknown, unknown, default);
        _ = await db.LocationAlerts.AsNoTracking().Take(1).ToArrayAsync();
        await new VaultDocumentRepository(db).GetDocumentsDueForReminderAsync(30, default);
        await new MedicalRepository(db).GetVaccinationsDueForReminderAsync(14, default);
        await new MedicalRepository(db).GetOverdueVaccinationsAsync(default);
        await new SafetyRepository(db).GetLateAlertEnabledZonesAsync(default);
    }

    [Theory]
    [InlineData("Asia/Kolkata", 11, 0, true)]   // 16:30 IST == 11:00 UTC → due at the alert minute
    [InlineData("Asia/Kolkata", 11, 9, true)]   // still inside the 10-minute window (drifted loop)
    [InlineData("Asia/Kolkata", 11, 10, false)] // window closed
    [InlineData("Asia/Kolkata", 16, 30, false)] // the old bug: UTC wall-clock match must NOT fire
    [InlineData(null, 11, 0, true)]             // missing timezone defaults to Asia/Kolkata
    public void Late_alert_is_due_in_the_family_local_time_window(string? timezone, int utcHour, int utcMinute, bool expected)
    {
        var zone = new FamilyFirst.Domain.Entities.SafeZone
        {
            LateAlertEnabled = true,
            LateAlertTime = new DateTime(2026, 1, 1, 16, 30, 0, DateTimeKind.Utc), // stored local wall-clock 16:30
            Family = new FamilyFirst.Domain.Entities.Family { TimezoneId = timezone! }
        };

        FamilyFirst.Infrastructure.Data.BackgroundServices.SafetyWorker
            .IsLateAlertDue(zone, new DateTime(2026, 11, 21, utcHour, utcMinute, 0, DateTimeKind.Utc))
            .Should().Be(expected);
    }

    private static FamilyFirstDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FamilyFirstDbContext>().UseSqlServer(SqlTestDatabase.ConnectionString()).Options);
}
