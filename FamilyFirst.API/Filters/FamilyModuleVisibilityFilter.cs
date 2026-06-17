using FamilyFirst.Application.Common.Exceptions;
using System.Security.Claims;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyFirst.API.Filters;

public sealed class FamilyModuleVisibilityFilter : IAsyncActionFilter
{
    private static readonly IReadOnlyDictionary<string, string> ModuleByController = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Families"] = "Family",
        ["Children"] = "Children",
        ["Attendance"] = "Attendance",
        ["CommentTemplates"] = "Attendance",
        ["Tasks"] = "Tasks",
        ["Rewards"] = "Rewards",
        ["Feedback"] = "Feedback",
        ["Calendar"] = "Calendar",
        ["Reports"] = "Reports",
        ["Notifications"] = "Notifications",
        ["FamilyAdmin"] = "FamilyAdmin"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;

        if (string.IsNullOrWhiteSpace(controllerName)
            || string.Equals(controllerName, "Auth", StringComparison.OrdinalIgnoreCase)
            || string.Equals(controllerName, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(controllerName, "FamilyAdmin", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (!context.RouteData.Values.TryGetValue("familyId", out var familyIdValue)
            || !Guid.TryParse(familyIdValue?.ToString(), out var familyId))
        {
            await next();
            return;
        }

        var currentRole = context.HttpContext.User.FindFirstValue(ClaimTypes.Role)
            ?? context.HttpContext.User.FindFirstValue("role");

        if (string.IsNullOrWhiteSpace(currentRole)
            || string.Equals(currentRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentRole, UserRole.FamilyAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (!Enum.TryParse<UserRole>(currentRole, true, out var role))
        {
            await next();
            return;
        }

        if (!ModuleByController.TryGetValue(controllerName, out var moduleName))
        {
            await next();
            return;
        }

        var repository = context.HttpContext.RequestServices.GetRequiredService<IFamilyAdminConfigRepository>();
        var configs = await repository.ListModuleVisibilityConfigsAsync(familyId, context.HttpContext.RequestAborted);
        var familyConfig = configs.FirstOrDefault(config =>
            config.Family is not null
            && config.Family.Id == familyId
            && config.RoleId == (int)role
            && string.Equals(config.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));
        var defaultConfig = configs.FirstOrDefault(config =>
            !config.FamilyId.HasValue
            && config.RoleId == (int)role
            && string.Equals(config.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));

        if ((familyConfig is not null && !familyConfig.IsVisible)
            || (familyConfig is null && defaultConfig is not null && !defaultConfig.IsVisible))
        {
            throw new ForbiddenAccessException("This module is hidden for the current role in this family.");
        }

        await next();
    }
}
