using System.Security.Cryptography;
using System.Text;
using FamilyFirst.API.Configuration;
using FamilyFirst.API.Filters;
using FamilyFirst.API.Middleware;
using FamilyFirst.Application.Validators;
using FamilyFirst.Domain.Enums;
using FamilyFirst.Infrastructure.Data.BackgroundServices;
using FamilyFirst.Infrastructure;
using FamilyFirst.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Development without a real Jwt:Secret (user-secrets / env var) must never sign tokens with the public
// placeholder from appsettings.Development.json. Use an ephemeral per-process key instead.
var usesEphemeralJwtSecret = false;
if (builder.Environment.IsDevelopment()
    && !(IntegrationSettings.IsConfigured(builder.Configuration["Jwt:Secret"])
         && builder.Configuration["Jwt:Secret"]!.Trim().Length >= 32))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
    });
    usesEphemeralJwtSecret = true;
}

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .Validate(options => builder.Environment.IsDevelopment()
        || (IntegrationSettings.IsConfigured(options.Secret) && options.Secret.Trim().Length >= 32),
        "Jwt:Secret must be configured with at least 32 characters outside Development.")
    .ValidateOnStart();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddScoped<FamilyModuleVisibilityFilter>();
builder.Services.AddValidatorsFromAssemblyContaining<SendOtpRequestValidator>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<FamilyModuleVisibilityFilter>();
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddHostedService<ReminderDeliveryWorker>();
builder.Services.AddHostedService<BirthdayEventGeneratorWorker>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();
builder.Services.AddHostedService<MorningDigestWorker>();
builder.Services.AddHostedService<EveningDigestWorker>();
builder.Services.AddHostedService<WeeklyDigestWorker>();
builder.Services.AddHostedService<OtpCleanupWorker>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = CreateJwtSigningKey(builder.Configuration["Jwt:Secret"]),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "SuperAdmin",
        policy => policy.RequireAssertion(context =>
            context.User.IsInRole(UserRole.SuperAdmin.ToString())
            || string.Equals(
                context.User.FindFirst("role")?.Value,
                UserRole.SuperAdmin.ToString(),
                StringComparison.OrdinalIgnoreCase)));
});

var app = builder.Build();

if (usesEphemeralJwtSecret)
{
    app.Logger.LogWarning(
        "Jwt:Secret is not configured; using an ephemeral development key (access tokens reset on restart). " +
        "Set one with: dotnet user-secrets set \"Jwt:Secret\" \"<32+ chars>\".");
}

if (!IntegrationSettings.IsConfigured(app.Configuration["Fcm:ProjectId"])
    || !(IntegrationSettings.IsConfigured(app.Configuration["Fcm:ServiceAccountJson"])
        || (IntegrationSettings.IsConfigured(app.Configuration["Fcm:ServiceAccountFilePath"])
            && File.Exists(app.Configuration["Fcm:ServiceAccountFilePath"]))))
{
    app.Logger.LogWarning("FCM requires Fcm:ProjectId and a service account to deliver push notifications.");
}

if (!app.Environment.IsDevelopment()
    && (!IntegrationSettings.IsConfigured(app.Configuration["Otp:ApiKey"])
        || !IntegrationSettings.IsConfigured(app.Configuration["Otp:TemplateId"])))
{
    app.Logger.LogWarning("OTP delivery requires Otp:ApiKey and Otp:TemplateId outside Development.");
}

if (!IntegrationSettings.IsConfigured(app.Configuration["Aws:BucketName"])
    || !IntegrationSettings.IsConfigured(app.Configuration["Aws:Region"]))
{
    app.Logger.LogWarning("S3 presigned uploads require Aws:BucketName and Aws:Region.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

static SymmetricSecurityKey CreateJwtSigningKey(string? secret)
{
    if (string.IsNullOrWhiteSpace(secret))
    {
        throw new InvalidOperationException("JWT secret is missing.");
    }

    return new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
