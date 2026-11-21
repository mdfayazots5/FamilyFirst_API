using System.Data;
using FamilyFirst.Application.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Infrastructure.Data.Repositories.Implementations;

public sealed class SqlOtpStore(IConfiguration configuration) : IOtpStore
{
    private const int RequestWindowMinutes = 60;
    private const int RequestLimit = 3;
    private const int LifetimeMinutes = 5;
    private const int AttemptLimit = 5;

    public Task<bool> TryCreateAsync(byte[] phoneHash, byte[] tokenHash, byte[] otpHash, CancellationToken cancellationToken)
    {
        return ExecuteAsync("dbo.uspInsertOtpRequest", phoneHash, tokenHash, otpHash, parameters =>
        {
            parameters.Add("@WindowMinutes", SqlDbType.Int).Value = RequestWindowMinutes;
            parameters.Add("@RequestLimit", SqlDbType.Int).Value = RequestLimit;
            parameters.Add("@LifetimeMinutes", SqlDbType.Int).Value = LifetimeMinutes;
        }, cancellationToken);
    }

    public Task<bool> TryVerifyAsync(byte[] phoneHash, byte[] tokenHash, byte[] otpHash, CancellationToken cancellationToken)
    {
        return ExecuteAsync("dbo.uspUpdateOtpRequestByTokenHash", phoneHash, tokenHash, otpHash, parameters =>
        {
            parameters.Add("@AttemptLimit", SqlDbType.Int).Value = AttemptLimit;
        }, cancellationToken);
    }

    public async Task<int> PurgeBatchOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("SqlServer"));
        await using var command = new SqlCommand("dbo.uspDeleteOtpRequestByAge", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@CutoffUtc", SqlDbType.DateTime2).Value = cutoffUtc;
        command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = batchSize;
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    private async Task<bool> ExecuteAsync(
        string procedure, byte[] phoneHash, byte[] tokenHash, byte[] otpHash,
        Action<SqlParameterCollection> addParameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("SqlServer"));
        await using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@PhoneNumberHash", SqlDbType.Binary, 32).Value = phoneHash;
        command.Parameters.Add("@TokenHash", SqlDbType.Binary, 32).Value = tokenHash;
        command.Parameters.Add("@OtpHash", SqlDbType.Binary, 32).Value = otpHash;
        addParameters(command.Parameters);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken) is Guid;
    }
}
