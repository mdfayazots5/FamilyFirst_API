using System.Security.Claims;
using FamilyFirst.Application.Common.Models;
using FamilyFirst.Application.DTOs.Auth;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyFirst.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> LoginWithPassword(
        LoginWithPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginWithPasswordAsync(request, cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Success(response, "Login successful."));
    }

    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SendOtpResponse>>> SendOtp(
        SendOtpRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.SendOtpAsync(request, cancellationToken);

        return Ok(ApiResponse<SendOtpResponse>.Success(response, "OTP sent."));
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyOtp(
        VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.VerifyOtpAsync(request, cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Success(response, "OTP verified."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Success(response, "Token refreshed."));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeToken(
        RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RevokeTokenAsync(request, cancellationToken);

        return Ok(ApiResponse<bool>.Success(response, "Token revoked."));
    }

    [HttpPost("set-pin")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> SetPin(
        SetPinRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.SetPinAsync(GetCurrentUserId(), request, cancellationToken);

        return Ok(ApiResponse<bool>.Success(response, "PIN set."));
    }

    [HttpPost("verify-pin")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyPin(
        VerifyPinRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.VerifyPinAsync(request, cancellationToken);

        return Ok(ApiResponse<AuthResponse>.Success(response, "PIN verified."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken)
    {
        var response = await _authService.GetCurrentUserAsync(User, cancellationToken);

        return Ok(ApiResponse<CurrentUserDto>.Success(response));
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
    }
}
