using System.Data;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using FamilyFirst.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

/// <summary>
/// Fine-grained per-operation permission check service.
/// Wraps: uspCheckRolePermission
///
/// Usage in any service method BEFORE a write, approve, or delete operation:
///
///   // Teacher trying to submit attendance
///   var allowed = await _permissionService.CheckAsync(
///       currentRole,
///       FamilyFirstModule.Attendance,
///       FamilyFirstPermission.CreateUpdate,
///       cancellationToken);
///
///   if (!allowed)
///       throw new ForbiddenAccessException(
///           await _errorCodeService.GetMessageAsync(FamilyFirstErrorCode.Permission_Denied));
///
/// Enum int values are used as @RoleId, @ModuleId, @PermissionId SP parameters.
/// Result is cached per (role, module, permission) within the request scope if needed.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly string _connectionString;
    private readonly ILogger<PermissionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly StartupCache _startupCache;

    public PermissionService(IConfiguration configuration, ILogger<PermissionService> logger, IMemoryCache cache, StartupCache startupCache)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' is missing.");
        _logger = logger;
        _cache = cache;
        _startupCache = startupCache;
    }

    public Task InvalidateAsync(UserRole role, FamilyFirstModule module)
    {
        _startupCache.Invalidate(StartupCache.Permission((int)role, (int)module));
        return Task.CompletedTask;
    }

    public async Task<bool> CheckAsync(
        UserRole role,
        FamilyFirstModule module,
        FamilyFirstPermission permission,
        CancellationToken cancellationToken = default)
    {
        var roleId       = (int)role;
        var moduleId     = (int)module;
        var permissionId = (int)permission;
        var generation = _startupCache.Generation;
        var key = StartupCache.Permission(roleId, moduleId);
        if (_cache.TryGetValue(key, out HashSet<long>? grants) && grants is not null)
            return grants.Contains(permissionId);

        var decisionKey = $"{key}:{permissionId}:{generation}";
        if (_cache.TryGetValue(decisionKey, out bool cached))
            return cached;

        _logger.LogDebug("[{Service}] Step 1 — Calling uspCheckRolePermission. Role={Role} Module={Module} Permission={Permission}",
            nameof(PermissionService), role, module, permission);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.uspCheckRolePermission", connection)
        {
            CommandType    = CommandType.StoredProcedure,
            CommandTimeout = 5
        };

        command.Parameters.Add("@RoleId",       SqlDbType.BigInt).Value = roleId;
        command.Parameters.Add("@ModuleId",      SqlDbType.BigInt).Value = moduleId;
        command.Parameters.Add("@PermissionId",  SqlDbType.BigInt).Value = permissionId;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        var isAuthorized = result is not null and not DBNull && Convert.ToBoolean(result);
        _startupCache.Remember(generation, decisionKey, isAuthorized);

        _logger.LogDebug("[{Service}] Step 2 — Result. Role={Role} Module={Module} Permission={Permission} IsAuthorized={IsAuthorized}",
            nameof(PermissionService), role, module, permission, isAuthorized);

        return isAuthorized;
    }

    public async Task<bool> CheckAsync(
        string roleName,
        FamilyFirstModule module,
        FamilyFirstPermission permission,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var role))
        {
            _logger.LogWarning("[{Service}] Unrecognised role name '{RoleName}' — denying permission",
                nameof(PermissionService), roleName);
            return false;
        }

        return await CheckAsync(role, module, permission, cancellationToken);
    }
}
