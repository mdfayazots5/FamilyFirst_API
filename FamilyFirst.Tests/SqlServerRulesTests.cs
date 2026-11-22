using System.Data;
using System.Security.Cryptography;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Infrastructure.Data;
using FamilyFirst.Infrastructure.Data.Repositories.Implementations;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Tests;

public sealed class SqlServerRulesTests
{
    [SqlServerFact]
    public async Task First_notification_preference_requests_reuse_one_row()
    {
        var childId = await SeedChildAsync();
        var options = new DbContextOptionsBuilder<FamilyFirstDbContext>().UseSqlServer(ConnectionString()).Options;
        await using var firstContext = new FamilyFirstDbContext(options);
        await using var secondContext = new FamilyFirstDbContext(options);
        var child = await firstContext.ChildProfiles.SingleAsync(profile => profile.Id == childId);
        var firstRepository = new NotificationPreferenceRepository(firstContext);
        var secondRepository = new NotificationPreferenceRepository(secondContext);

        var results = await Task.WhenAll(
            firstRepository.AddOrGetAsync(new NotificationPreference { UserId = child.UserId, FamilyId = child.FamilyId }, default),
            secondRepository.AddOrGetAsync(new NotificationPreference { UserId = child.UserId, FamilyId = child.FamilyId }, default));

        results[0].Id.Should().NotBeEmpty();
        results[1].Id.Should().Be(results[0].Id);
        (await firstContext.Set<NotificationPreference>().CountAsync(preference => preference.UserId == child.UserId))
            .Should().Be(1);
    }

    [SqlServerFact]
    public async Task Otp_expires_after_five_minutes_and_cannot_be_verified_after_expiry()
    {
        var store = CreateOtpStore();
        var phone = RandomNumberGenerator.GetBytes(32);
        var token = RandomNumberGenerator.GetBytes(32);
        var code = RandomNumberGenerator.GetBytes(32);
        (await store.TryCreateAsync(phone, token, code, default)).Should().BeTrue();

        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DATEDIFF(SECOND, DateCreated, ExpiresAt) FROM dbo.tblOtpRequest WHERE TokenHash = @token";
        command.Parameters.Add("@token", SqlDbType.Binary, 32).Value = token;
        (await command.ExecuteScalarAsync()).Should().Be(300);

        command.CommandText = "UPDATE dbo.tblOtpRequest SET ExpiresAt = DATEADD(SECOND, -1, SYSUTCDATETIME()) WHERE TokenHash = @token";
        (await command.ExecuteNonQueryAsync()).Should().Be(1);
        (await store.TryVerifyAsync(phone, token, code, default)).Should().BeFalse();

        var freshToken = RandomNumberGenerator.GetBytes(32);
        (await store.TryCreateAsync(phone, freshToken, code, default)).Should().BeTrue();
        (await store.TryVerifyAsync(phone, freshToken, code, default)).Should().BeTrue();
    }

    [SqlServerFact]
    public async Task Concurrent_otp_requests_reserve_only_three_slots_per_hour()
    {
        var store = CreateOtpStore();
        var phone = RandomNumberGenerator.GetBytes(32);
        var results = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => store.TryCreateAsync(phone, RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), default)));
        results.Count(result => result).Should().Be(3);

        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.tblOtpRequest SET DateCreated = DATEADD(MINUTE, -61, SYSUTCDATETIME()) WHERE PhoneNumberHash = @phone";
        command.Parameters.Add("@phone", SqlDbType.Binary, 32).Value = phone;
        (await command.ExecuteNonQueryAsync()).Should().Be(3);
        (await store.TryCreateAsync(phone, RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), default)).Should().BeTrue();
    }

    [SqlServerFact]
    public async Task Otp_purge_hard_deletes_rows_older_than_cutoff_and_keeps_fresh_rows()
    {
        var store = CreateOtpStore();
        var phone = RandomNumberGenerator.GetBytes(32);
        var oldToken = RandomNumberGenerator.GetBytes(32);
        var freshToken = RandomNumberGenerator.GetBytes(32);
        (await store.TryCreateAsync(phone, oldToken, RandomNumberGenerator.GetBytes(32), default)).Should().BeTrue();
        (await store.TryCreateAsync(phone, freshToken, RandomNumberGenerator.GetBytes(32), default)).Should().BeTrue();

        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.tblOtpRequest SET DateCreated = DATEADD(HOUR, -25, SYSUTCDATETIME()) WHERE TokenHash = @old";
        command.Parameters.Add("@old", SqlDbType.Binary, 32).Value = oldToken;
        (await command.ExecuteNonQueryAsync()).Should().Be(1);

        var removed = await store.PurgeBatchOlderThanAsync(DateTime.UtcNow.AddHours(-24), 5000, default);
        removed.Should().BeGreaterThanOrEqualTo(1);

        command.CommandText = "SELECT COUNT(1) FROM dbo.tblOtpRequest WHERE TokenHash IN (@old, @fresh)";
        command.Parameters.Add("@fresh", SqlDbType.Binary, 32).Value = freshToken;
        (await command.ExecuteScalarAsync()).Should().Be(1, "only the fresh challenge survives");
    }

    [SqlServerTheory]
    [InlineData(30, -20)]
    [InlineData(-30, 20)]
    public async Task RowVersion_rejects_stale_coin_change_and_rolls_back_its_ledger_row(int first, int stale)
    {
        var childId = await SeedChildAsync();
        var options = new DbContextOptionsBuilder<FamilyFirstDbContext>().UseSqlServer(ConnectionString()).Options;
        await using var winner = new FamilyFirstDbContext(options);
        await using var loser = new FamilyFirstDbContext(options);
        var current = await winner.ChildProfiles.AsNoTracking().SingleAsync(child => child.Id == childId);
        var old = await loser.ChildProfiles.AsNoTracking().SingleAsync(child => child.Id == childId);
        var originalVersion = current.RowVersion.ToArray();
        var accepted = CreateCoinChange(current, first);
        var rejected = CreateCoinChange(old, stale);

        await new CoinTransactionRepository(winner).ApplyAsync(current, accepted, default);
        Func<Task> attempt = () => new CoinTransactionRepository(loser).ApplyAsync(old, rejected, default);
        await attempt.Should().ThrowAsync<DbUpdateConcurrencyException>();

        await using var verify = new FamilyFirstDbContext(options);
        var saved = await verify.ChildProfiles.AsNoTracking().SingleAsync(child => child.Id == childId);
        saved.CoinBalance.Should().Be(100 + first);
        saved.RowVersion.Should().NotEqual(originalVersion);
        var ledger = await verify.CoinTransactions.Where(entry => entry.ChildProfileId == saved.InternalId).ToArrayAsync();
        ledger.Should().ContainSingle().Which.Amount.Should().Be(first);
    }

    private static CoinTransaction CreateCoinChange(ChildProfile profile, int amount)
    {
        profile.CoinBalance += amount;
        if (amount > 0) profile.TotalCoinsEarned += amount;
        return new CoinTransaction
        {
            ChildProfileId = profile.InternalId, FamilyId = profile.FamilyId, Amount = amount,
            BalanceAfter = profile.CoinBalance, TransactionType = amount > 0 ? "Earned" : "Spent",
            ReferenceType = "Test", CreatedByUserId = profile.UserId
        };
    }

    private static SqlOtpStore CreateOtpStore() => new(new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["ConnectionStrings:SqlServer"] = ConnectionString() }).Build());

    private static string ConnectionString() => SqlTestDatabase.ConnectionString();

    private static async Task<Guid> SeedChildAsync()
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            INSERT dbo.tblUser (PhoneNumber, CountryCode, FullName) VALUES (@phone, N'+91', N'Phase 13 test account');
            DECLARE @user BIGINT = SCOPE_IDENTITY();
            INSERT dbo.tblFamily (FamilyName, JoinCode, PlanId, FamilyAdminUserId)
            VALUES (N'Phase 13 test family', @join, (SELECT MIN(PlanId) FROM dbo.tblPlan WHERE IsDeleted = 0), @user);
            DECLARE @family BIGINT = SCOPE_IDENTITY();
            INSERT dbo.tblFamilyMember (FamilyId, UserId, Role, LinkType) VALUES (@family, @user, 4, N'Son');
            DECLARE @member BIGINT = SCOPE_IDENTITY();
            INSERT dbo.tblChildProfile (FamilyMemberId, UserId, FamilyId, CoinBalance)
            VALUES (@member, @user, @family, 100);
            DECLARE @child BIGINT = SCOPE_IDENTITY();
            COMMIT TRANSACTION;
            SELECT Id FROM dbo.tblChildProfile WHERE ChildProfileId = @child;
            """;
        command.Parameters.Add("@phone", SqlDbType.NVarChar, 24).Value = $"+919{RandomNumberGenerator.GetInt32(100_000_000, 999_999_999)}";
        command.Parameters.Add("@join", SqlDbType.NVarChar, 16).Value = Guid.NewGuid().ToString("N")[..12];
        return (Guid)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Test child was not created."));
    }
}

public static class SqlTestDatabase
{
    public static string ConnectionString()
    {
        var database = Environment.GetEnvironmentVariable("FAMILYFIRST_TEST_DATABASE");
        if (database is null || !System.Text.RegularExpressions.Regex.IsMatch(database, @"\AFamilyFirstP13A_[0-9]{14}\z"))
            throw new InvalidOperationException("Use a disposable FamilyFirstP13A_yyyyMMddHHmmss LocalDB database.");
        return new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = database,
            IntegratedSecurity = true, TrustServerCertificate = true
        }.ConnectionString;
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FAMILYFIRST_TEST_DATABASE")))
            Skip = "Set FAMILYFIRST_TEST_DATABASE to a disposable LocalDB database initialized by RunAllScripts.ps1.";
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FAMILYFIRST_TEST_DATABASE")))
            Skip = "Set FAMILYFIRST_TEST_DATABASE to a disposable LocalDB database initialized by RunAllScripts.ps1.";
    }
}
