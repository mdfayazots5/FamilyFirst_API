using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Security;
using FamilyFirst.Application.DTOs.Auth;
using FamilyFirst.Application.Services.Implementations;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using FamilyFirst.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FamilyFirst.Tests;

/// <summary>
/// R2 authentication rules (ProjectOverview §2.4 rules 12, 19): credential lockout, Child/Elder-only PIN sign-in,
/// inactive-user rejection, counter reset on success.
/// </summary>
public sealed class AuthSecurityTests
{
    private const string Pin = "2580";
    private const string Password = "S3cure-pass";

    [Fact]
    public async Task Correct_child_pin_signs_in_and_resets_the_failure_counter()
    {
        var setup = new Setup(UserRole.Child);
        setup.User.FailedLoginAttemptCount = 3;

        var response = await setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = Pin }, default);

        response.AccessToken.Should().Be("access-token");
        setup.Users.Verify(r => r.UpdateAsync(It.Is<User>(u => u.FailedLoginAttemptCount == 0 && u.LockoutUntil == null), default), Times.Once);
        setup.Users.Verify(r => r.RegisterFailedLoginAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task Elder_pin_is_allowed()
    {
        var setup = new Setup(UserRole.Elder);
        var response = await setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = Pin }, default);
        response.AccessToken.Should().Be("access-token");
    }

    [Theory]
    [InlineData(UserRole.Parent)]
    [InlineData(UserRole.FamilyAdmin)]
    [InlineData(UserRole.Teacher)]
    public async Task Pin_sign_in_is_refused_for_adult_roles_even_with_the_correct_pin(UserRole role)
    {
        var setup = new Setup(role);
        Func<Task> act = () => setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = Pin }, default);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Wrong_pin_records_one_atomic_failure_with_the_documented_policy()
    {
        var setup = new Setup(UserRole.Child);
        var before = DateTime.UtcNow;

        Func<Task> act = () => setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = "1357" }, default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        setup.Users.Verify(r => r.RegisterFailedLoginAsync(
            setup.User.InternalId,
            AuthService.MaxFailedLoginAttempts,
            It.Is<DateTime>(until => until >= before.Add(AuthService.LoginLockoutDuration) && until <= DateTime.UtcNow.Add(AuthService.LoginLockoutDuration)),
            default), Times.Once);
        AuthService.MaxFailedLoginAttempts.Should().Be(5);
        AuthService.LoginLockoutDuration.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Locked_account_is_refused_with_Account_Locked_even_for_the_correct_pin()
    {
        var setup = new Setup(UserRole.Child);
        setup.User.LockoutUntil = DateTime.UtcNow.AddMinutes(10);

        Func<Task> act = () => setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = Pin }, default);

        (await act.Should().ThrowAsync<TooManyRequestsException>()).Which.ErrorCode.Should().Be(FamilyFirstErrorCode.Account_Locked);
        setup.Users.Verify(r => r.RegisterFailedLoginAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<DateTime>(), default), Times.Never);
        setup.Users.Verify(r => r.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task Expired_lockout_no_longer_blocks_sign_in()
    {
        var setup = new Setup(UserRole.Child);
        setup.User.LockoutUntil = DateTime.UtcNow.AddSeconds(-1);
        var response = await setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = Pin }, default);
        response.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task Inactive_user_cannot_sign_in_with_pin()
    {
        var setup = new Setup(UserRole.Child);
        setup.User.IsActive = false;
        Func<Task> act = () => setup.Auth().VerifyPinAsync(new VerifyPinRequest { UserId = setup.User.Id, Pin = Pin }, default);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Wrong_password_records_a_failure_and_locked_password_account_is_refused()
    {
        var setup = new Setup(UserRole.Parent);
        Func<Task> wrong = () => setup.Auth().LoginWithPasswordAsync(new LoginWithPasswordRequest(setup.User.PhoneNumber, "+91", "nope"), default);
        await wrong.Should().ThrowAsync<UnauthorizedAccessException>();
        setup.Users.Verify(r => r.RegisterFailedLoginAsync(setup.User.InternalId, 5, It.IsAny<DateTime>(), default), Times.Once);

        setup.User.LockoutUntil = DateTime.UtcNow.AddMinutes(5);
        Func<Task> locked = () => setup.Auth().LoginWithPasswordAsync(new LoginWithPasswordRequest(setup.User.PhoneNumber, "+91", Password), default);
        await locked.Should().ThrowAsync<TooManyRequestsException>();
    }

    [Fact]
    public void Pin_hasher_is_salted_and_verifies_only_the_original_secret()
    {
        var first = PinHasher.Hash(Pin);
        var second = PinHasher.Hash(Pin);
        first.Should().NotBe(second, "each hash carries its own random salt");
        PinHasher.Verify(Pin, first).Should().BeTrue();
        PinHasher.Verify("2581", first).Should().BeFalse();
        PinHasher.Verify(Pin, "not-a-hash").Should().BeFalse();
    }

    private sealed class Setup
    {
        private readonly Dictionary<Type, Mock> _mocks = [];
        public User User { get; }
        public Mock<IUserRepository> Users => Mock<IUserRepository>();

        public Setup(UserRole role)
        {
            User = new User
            {
                InternalId = 42,
                PhoneNumber = "+919000000099",
                PinHash = PinHasher.Hash(Pin),
                PasswordHash = PinHasher.Hash(Password),
                IsActive = true,
            };
            var membership = new FamilyMember { Role = role, UserId = User.InternalId, FamilyId = 7, Family = new Family { InternalId = 7 } };

            Users.Setup(r => r.GetByIdAsync(User.Id, default)).ReturnsAsync(User);
            Users.Setup(r => r.GetByPhoneNumberAsync(User.PhoneNumber, default)).ReturnsAsync(User);
            Mock<IFamilyMemberRepository>().Setup(r => r.GetPrimaryActiveMembershipForUserAsync(User.Id, default)).ReturnsAsync(membership);
            Mock<IJwtTokenService>().Setup(s => s.GenerateAccessToken(It.IsAny<User>(), It.IsAny<AuthTokenContext>())).Returns("access-token");
            Mock<IJwtTokenService>().Setup(s => s.GenerateRefreshToken()).Returns("refresh-token");
            Mock<IJwtTokenService>().Setup(s => s.HashToken(It.IsAny<string>())).Returns("hash");
            Mock<IErrorCodeService>().Setup(s => s.GetMessageAsync(It.IsAny<FamilyFirstErrorCode>(), It.IsAny<int>(), default))
                .ReturnsAsync((FamilyFirstErrorCode code, int _, CancellationToken _) => code.ToString());
        }

        public Mock<T> Mock<T>() where T : class
        {
            if (!_mocks.TryGetValue(typeof(T), out var mock))
                _mocks.Add(typeof(T), mock = new Mock<T>());
            return (Mock<T>)mock;
        }

        private T Get<T>() where T : class => Mock<T>().Object;

        public AuthService Auth() => new(Get<IUserRepository>(), Get<IRefreshTokenRepository>(), Get<IFamilyMemberRepository>(),
            Get<IChildProfileRepository>(), Get<ITeacherProfileRepository>(), Get<ITeacherChildAssignmentRepository>(),
            Get<IOtpService>(), Get<IJwtTokenService>(), Get<IApiLogService>(), Get<IErrorCodeService>());
    }
}
