using FamilyFirst.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FamilyFirst.Tests;

/// <summary>
/// R3/R4 storage rules (ProjectOverview §6.4 rule 14, §12.4): client-supplied URLs must sit inside the family/task
/// prefix, and stored URLs leave the API only as presigned GETs. Presigning is local signature math (no network);
/// dummy credentials are supplied via the AWS environment variables.
/// </summary>
public sealed class S3ObjectUrlTests
{
    private const string Bucket = "ff-test-bucket";
    private const string Region = "ap-south-1";
    private static readonly Guid FamilyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TaskId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly string Root = $"https://{Bucket}.s3.{Region}.amazonaws.com/";

    static S3ObjectUrlTests()
    {
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "AKIAEXAMPLETESTKEY00");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "testsecretkeyexample0000000000000000000000");
    }

    private static IConfiguration Config(bool configured = true) => new ConfigurationBuilder()
        .AddInMemoryCollection(configured
            ? new Dictionary<string, string?> { ["Aws:BucketName"] = Bucket, ["Aws:Region"] = Region }
            : new Dictionary<string, string?>())
        .Build();

    [Theory]
    [InlineData("family/11111111-1111-1111-1111-111111111111/vault/identity/a.pdf", true)]
    [InlineData("family/00000000-0000-0000-0000-000000000001/vault/identity/a.pdf", false)] // other family
    [InlineData("family/11111111-1111-1111-1111-111111111111/tasks/x.jpg", false)]          // wrong area
    [InlineData("family/11111111-1111-1111-1111-111111111111/vault/../../x.pdf", false)]    // traversal
    [InlineData("family/11111111-1111-1111-1111-111111111111/vault/a.pdf?x=1", false)]      // query smuggling
    public void Vault_file_url_must_be_inside_the_family_vault_prefix(string key, bool expected)
    {
        new VaultStorageService(Config()).IsFamilyObjectUrl(FamilyId, Root + key).Should().Be(expected);
    }

    [Fact]
    public void External_urls_are_rejected_when_storage_is_configured()
    {
        new VaultStorageService(Config()).IsFamilyObjectUrl(FamilyId, "https://evil.example.com/a.pdf").Should().BeFalse();
    }

    [Fact]
    public void Unconfigured_storage_accepts_https_only_for_local_development()
    {
        var service = new VaultStorageService(Config(configured: false));
        service.IsFamilyObjectUrl(FamilyId, "https://localhost/fixture.pdf").Should().BeTrue();
        service.IsFamilyObjectUrl(FamilyId, "http://localhost/fixture.pdf").Should().BeFalse();
    }

    [Theory]
    [InlineData("family/11111111-1111-1111-1111-111111111111/tasks/22222222-2222-2222-2222-222222222222/p.jpg", true)]
    [InlineData("family/11111111-1111-1111-1111-111111111111/tasks/99999999-9999-9999-9999-999999999999/p.jpg", false)]
    [InlineData("family/00000000-0000-0000-0000-000000000001/tasks/22222222-2222-2222-2222-222222222222/p.jpg", false)]
    public void Task_photo_url_must_be_inside_the_task_prefix(string key, bool expected)
    {
        new S3StorageService(Config()).IsTaskPhotoUrl(FamilyId, TaskId, Root + key).Should().Be(expected);
    }

    [Fact]
    public void Stored_bucket_urls_are_returned_as_presigned_gets_and_other_urls_unchanged()
    {
        var service = new VaultStorageService(Config());
        var stored = Root + "family/11111111-1111-1111-1111-111111111111/vault/identity/a.pdf";

        var read = service.CreateReadUrl(stored);

        read.Should().StartWith(stored).And.Contain("X-Amz-Signature=").And.Contain("X-Amz-Expires=3600");
        service.CreateReadUrl("https://picsum.photos/seed/x/800/600").Should().Be("https://picsum.photos/seed/x/800/600");
        new VaultStorageService(Config(configured: false)).CreateReadUrl(stored).Should().Be(stored);
    }
}
