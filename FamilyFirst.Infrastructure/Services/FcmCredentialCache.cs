using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Infrastructure.Services;

/// <summary>
/// Singleton holder for the scoped FCM service-account credential. Loading the JSON/file and creating the scoped
/// credential happens once; the Google library caches the access token and refreshes it before expiry, so each push
/// no longer pays a credential load + token round trip (matters for the &lt;3 s SOS path).
/// </summary>
public sealed class FcmCredentialCache
{
    private const string MessagingScope = "https://www.googleapis.com/auth/firebase.messaging";
    private readonly Lazy<ITokenAccess?> _credential;

    public FcmCredentialCache(IConfiguration configuration)
    {
        _credential = new Lazy<ITokenAccess?>(() => Load(configuration), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken) =>
        _credential.Value is { } credential
            ? credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken)!
            : Task.FromResult<string?>(null);

    private static ITokenAccess? Load(IConfiguration configuration)
    {
        var serviceAccountJson = configuration["Fcm:ServiceAccountJson"];
        var serviceAccountFilePath = configuration["Fcm:ServiceAccountFilePath"];

        GoogleCredential? credential = null;
        if (IntegrationSettings.IsConfigured(serviceAccountJson))
            credential = GoogleCredential.FromJson(serviceAccountJson);
        else if (IntegrationSettings.IsConfigured(serviceAccountFilePath) && File.Exists(serviceAccountFilePath))
            credential = GoogleCredential.FromFile(serviceAccountFilePath);

        return credential?.CreateScoped(MessagingScope).UnderlyingCredential;
    }
}
