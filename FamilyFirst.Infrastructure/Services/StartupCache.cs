using System.Text.RegularExpressions;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FamilyFirst.Infrastructure.Services;

public sealed class StartupCache(IMemoryCache cache) : IRegexRuleProvider
{
    private readonly object _invalidationLock = new();
    private long _generation;
    internal long Generation => Interlocked.Read(ref _generation);

    internal void Invalidate(string key)
    {
        lock (_invalidationLock)
        {
            Interlocked.Increment(ref _generation);
            cache.Remove(key);
        }
    }

    internal void Remember<T>(long generation, string key, T value)
    {
        lock (_invalidationLock)
        {
            if (_generation == generation)
                cache.Set(key, value, Lifetime);
        }
    }
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    internal static string Error(int code, int language) => $"ErrorCode:{code}:{language}";
    internal static string Permission(long role, long module) => $"RolePermission:{role}:{module}";
    internal static string Master(string code) => $"MasterData:{code}";
    internal static string Method(string method) => $"ApiMethod:{method}";
    internal static string Pattern(long method, string field) => $"RegexPattern:{method}:{field}";

    public Regex? Find(string method, string field) =>
        cache.TryGetValue(Method(method), out long id)
            ? cache.Get<Regex>(Pattern(id, field))
            : null;
}
