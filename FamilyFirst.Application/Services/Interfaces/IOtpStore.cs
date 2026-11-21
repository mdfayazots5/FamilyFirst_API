namespace FamilyFirst.Application.Services.Interfaces;

public interface IOtpStore
{
    Task<bool> TryCreateAsync(byte[] phoneHash, byte[] tokenHash, byte[] otpHash, CancellationToken cancellationToken);

    Task<bool> TryVerifyAsync(byte[] phoneHash, byte[] tokenHash, byte[] otpHash, CancellationToken cancellationToken);

    /// <summary>Physically deletes one batch of OTP rows created before <paramref name="cutoffUtc"/>; returns rows removed.</summary>
    Task<int> PurgeBatchOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken);
}
