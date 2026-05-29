using FamilyFirst.Domain.Entities.Base;

namespace FamilyFirst.Domain.Entities;

public sealed class User : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = "+91";

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public string? PinHash { get; set; }

    public string? PasswordHash { get; set; }

    public string? FcmToken { get; set; }

    public bool IsPhoneVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public string PreferredLanguage { get; set; } = "en";

    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
}
