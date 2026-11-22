using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Admin;
using FamilyFirst.Application.DTOs.Notification;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FamilyFirst.Application.Services.Implementations;

public sealed class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly INotificationService _notificationService;
    private readonly IApiLogService _apiLogService;
    private readonly IPermissionService _permissionService;
    private readonly IErrorCodeService _errorCodeService;
    private readonly IMasterDataResolver _masterDataResolver;

    public AdminService(
        IAdminRepository adminRepository,
        INotificationService notificationService,
        IApiLogService apiLogService,
        IPermissionService permissionService,
        IErrorCodeService errorCodeService,
        IMasterDataResolver masterDataResolver)
    {
        _adminRepository = adminRepository;
        _notificationService = notificationService;
        _apiLogService = apiLogService;
        _permissionService = permissionService;
        _errorCodeService = errorCodeService;
        _masterDataResolver = masterDataResolver;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(string? currentUserRole, CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.AdminView, cancellationToken);
        var response = await _adminRepository.GetDashboardAsync(cancellationToken);
        LogApiCall(nameof(GetDashboardAsync), new { currentUserRole }, response);
        return response;
    }

    public async Task<PaginatedList<AdminFamilySummaryDto>> SearchFamiliesAsync(
        string? currentUserRole,
        AdminFamilySearchRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.AdminView, cancellationToken);
        var families = await _adminRepository.SearchFamiliesAsync(request, cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        var response = PaginatedList<AdminFamilySummaryDto>.Create(families, page, pageSize);
        LogApiCall(nameof(SearchFamiliesAsync), new { currentUserRole, request.Query, request.IsActive, request.Status, request.Page, request.PageSize }, new { response.TotalCount });
        return response;
    }

    public async Task<AdminFamilyDetailDto> GetFamilyDetailAsync(
        string? currentUserRole,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.AdminView, cancellationToken);
        await EnsureFamilyGuidValidAsync(familyId, cancellationToken);

        var response = await _adminRepository.GetFamilyDetailAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));
        LogApiCall(nameof(GetFamilyDetailAsync), new { currentUserRole, familyId }, new { response.FamilyId });
        return response;
    }

    public async Task<bool> BlockFamilyAsync(
        string? currentUserRole,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.Delete, cancellationToken);
        await EnsureFamilyGuidValidAsync(familyId, cancellationToken);

        var family = await _adminRepository.GetFamilyEntityByIdAsync(familyId, cancellationToken)
            ?? throw new NotFoundException(await GetMessageAsync(FamilyFirstErrorCode.Family_Not_Found, cancellationToken));
        var members = await _adminRepository.ListFamilyMemberEntitiesAsync(familyId, cancellationToken);

        family.IsActive = false;

        foreach (var member in members)
        {
            member.IsActive = false;
        }

        await _adminRepository.UpdateFamilyAndMembersAsync(family, members, cancellationToken);

        LogApiCall(nameof(BlockFamilyAsync), new { currentUserRole, familyId }, new { Success = true });
        return true;
    }

    public async Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.AdminView, cancellationToken);
        var response = await _adminRepository.GetAnalyticsOverviewAsync(cancellationToken);
        LogApiCall(nameof(GetAnalyticsOverviewAsync), new { currentUserRole }, response);
        return response;
    }

    public async Task<IReadOnlyCollection<FeatureFlagDto>> ListFeatureFlagsAsync(
        string? currentUserRole,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.AdminView, cancellationToken);
        var response = await _adminRepository.ListFeatureFlagsAsync(cancellationToken);
        LogApiCall(nameof(ListFeatureFlagsAsync), new { currentUserRole }, new { Count = response.Count });
        return response;
    }

    public async Task<FeatureFlagDto> UpdateFeatureFlagAsync(
        string? currentUserRole,
        string flagKey,
        UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.CreateUpdate, cancellationToken);

        flagKey = flagKey.Trim();
        if (flagKey.Length is 0 or > 100)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(flagKey)] = ["The flag key must contain between 1 and 100 characters."]
            });
        }

        var value = request.FlagValue.Trim();
        switch (flagKey.ToUpperInvariant())
        {
            case "MAINTENANCEMODE":
            case "GLOBALNOTIFICATIONS":
            case "GLOBALREPORTS":
                if (!bool.TryParse(value, out var enabled))
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        [nameof(request.FlagValue)] = ["Enter true or false for this flag."]
                    });
                }

                value = enabled ? "true" : "false";
                break;
            case "MINIMUMAPPVERSION":
                if (!Regex.IsMatch(value, @"\A[0-9]+\.[0-9]+\.[0-9]+\z") || !Version.TryParse(value, out _))
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        [nameof(request.FlagValue)] = ["Enter a version with three numbers, such as 1.2.0."]
                    });
                }

                break;
        }

        var featureFlag = await _adminRepository.GetFeatureFlagEntityByKeyAsync(flagKey, cancellationToken);

        if (featureFlag is null)
        {
            featureFlag = new Domain.Entities.FeatureFlag
            {
                FlagKey = flagKey,
                FlagValue = value,
                Description = NormalizeOptional(request.Description),
                LastUpdated = DateTime.UtcNow
            };

            await _adminRepository.AddFeatureFlagAsync(featureFlag, cancellationToken);
        }
        else
        {
            featureFlag.FlagValue = value;
            if (request.Description is not null)
            {
                featureFlag.Description = NormalizeOptional(request.Description);
            }
            featureFlag.LastUpdated = DateTime.UtcNow;
            await _adminRepository.UpdateFeatureFlagAsync(featureFlag, cancellationToken);
        }

        var response = new FeatureFlagDto(
            featureFlag.FlagKey,
            featureFlag.FlagValue,
            featureFlag.Description,
            featureFlag.LastUpdated ?? DateTime.MinValue);
        LogApiCall(nameof(UpdateFeatureFlagAsync), new { currentUserRole, flagKey }, response);
        return response;
    }

    public async Task<NotificationCampaignResultDto> SendCampaignAsync(
        string? currentUserRole,
        NotificationCampaignRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSuperAdminAsync(currentUserRole, FamilyFirstPermission.CreateUpdate, cancellationToken);

        var recipientUserIds = await _adminRepository.ListCampaignRecipientUserIdsAsync(
            request.Roles,
            cancellationToken);
        var notifications = recipientUserIds
            .Select(recipientUserId => new CreateNotificationRequest
            {
                RecipientUserId = recipientUserId,
                Title = request.Title.Trim(),
                Body = request.Body.Trim(),
                Priority = request.Priority,
                ReferenceType = "Campaign",
                DeepLinkPath = NormalizeOptional(request.DeepLinkPath),
                IsBatched = false,
                ScheduledFor = request.ScheduledFor?.UtcDateTime
            })
            .ToArray();

        if (notifications.Length > 0)
        {
            await _notificationService.CreateManyAsync(notifications, cancellationToken);
        }

        var response = new NotificationCampaignResultDto(notifications.Length);
        LogApiCall(nameof(SendCampaignAsync), new { currentUserRole, request.Title, Roles = request.Roles.Count }, new { response.RecipientCount });
        return response;
    }

    private async Task EnsureSuperAdminAsync(string? currentUserRole, FamilyFirstPermission permission, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(currentUserRole, true, out var role) || role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }

        var hasPermission = await _permissionService.CheckAsync(
            role,
            FamilyFirstModule.AdminConfiguration,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(await GetMessageAsync(FamilyFirstErrorCode.Permission_Denied, cancellationToken));
        }
    }

    private async Task EnsureFamilyGuidValidAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var resolvedFamilyId = await _masterDataResolver.ResolveAsync(
            MasterDataCodes.Family,
            familyId.ToString(),
            cancellationToken: cancellationToken);

        if (!resolvedFamilyId.HasValue)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(familyId)] = new[] { await GetMessageAsync(FamilyFirstErrorCode.Invalid_MasterData, cancellationToken) }
            });
        }
    }

    private async Task<string> GetMessageAsync(FamilyFirstErrorCode errorCode, CancellationToken cancellationToken)
    {
        return await _errorCodeService.GetMessageAsync(errorCode, cancellationToken: cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void LogApiCall(string methodName, object? request, object? response)
    {
        _apiLogService.Log(
            methodName,
            request is null ? null : JsonSerializer.Serialize(request),
            response is null ? null : JsonSerializer.Serialize(response));
    }
}
