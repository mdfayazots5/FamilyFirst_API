using System.Security.Cryptography;
using System.Text;
using FamilyFirst.API.Filters;
using FamilyFirst.API.Middleware;
using FamilyFirst.Application.Validators;
using FamilyFirst.Domain.Enums;
using FamilyFirst.Infrastructure.Data.BackgroundServices;
using FamilyFirst.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHostedService<ReminderDeliveryWorker>();
builder.Services.AddHostedService<BirthdayEventGeneratorWorker>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();
builder.Services.AddHostedService<MorningDigestWorker>();
builder.Services.AddHostedService<EveningDigestWorker>();
builder.Services.AddHostedService<WeeklyDigestWorker>();
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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
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
