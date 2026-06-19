namespace Aurora.Flowboard.Domain.UnitTests.Users;

public sealed class UserTests
{
    public sealed class Create : BaseTest
    {
        [Fact]
        public void Should_CreateUser_When_DataIsValid()
        {
            // Arrange
            Email email = Email.Create(UserData.EmailAddress).Value;

            // Act
            Result<User> result = User.Create(
                UserData.FirstName,
                UserData.LastName,
                email,
                UserData.Password,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.FirstName.Should().Be(UserData.FirstName);
            result.Value.LastName.Should().Be(UserData.LastName);
            result.Value.Email.Should().Be(email);
        }

        [Fact]
        public void Should_SetIsActiveToTrue_When_Created()
        {
            // Act
            User user = UserData.GetActiveUser();

            // Assert
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Should_SetEmptyCollections_When_Created()
        {
            // Act
            User user = UserData.GetActiveUser();

            // Assert
            user.Tokens.Should().BeEmpty();
            user.Roles.Should().BeEmpty();
        }

        [Fact]
        public void Should_SetCreatedOnUtc_When_Created()
        {
            // Act
            User user = UserData.GetActiveUser();

            // Assert
            user.CreatedOnUtc.Should().Be(UserData.CreatedOnUtc);
        }

        [Fact]
        public void Should_TrimFirstAndLastName_When_Creating()
        {
            // Arrange
            Email email = Email.Create(UserData.EmailAddress).Value;

            // Act
            Result<User> result = User.Create(
                $"  {UserData.FirstName}  ",
                $"  {UserData.LastName}  ",
                email,
                UserData.Password,
                UserData.CreatedOnUtc);

            // Assert
            result.Value.FirstName.Should().Be(UserData.FirstName);
            result.Value.LastName.Should().Be(UserData.LastName);
        }

        [Fact]
        public void Should_RaiseUserCreatedDomainEvent_When_Created()
        {
            // Act
            User user = UserData.GetActiveUser();

            // Assert
            UserCreatedDomainEvent domainEvent = AssertDomainEventWasPublished<UserCreatedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void Should_Fail_When_FirstNameIsEmpty()
        {
            // Arrange
            Email email = Email.Create(UserData.EmailAddress).Value;

            // Act
            Result<User> result = User.Create(
                string.Empty,
                UserData.LastName,
                email,
                UserData.Password,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.FirstNameRequired);
        }

        [Fact]
        public void Should_Fail_When_FirstNameIsWhitespace()
        {
            // Arrange
            Email email = Email.Create(UserData.EmailAddress).Value;

            // Act
            Result<User> result = User.Create(
                "   ",
                UserData.LastName,
                email,
                UserData.Password,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.FirstNameRequired);
        }

        [Fact]
        public void Should_Fail_When_LastNameIsEmpty()
        {
            // Arrange
            Email email = Email.Create(UserData.EmailAddress).Value;

            // Act
            Result<User> result = User.Create(
                UserData.FirstName,
                string.Empty,
                email,
                UserData.Password,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.LastNameRequired);
        }

        [Fact]
        public void Should_Fail_When_LastNameIsWhitespace()
        {
            // Arrange
            Email email = Email.Create(UserData.EmailAddress).Value;

            // Act
            Result<User> result = User.Create(
                UserData.FirstName,
                "   ",
                email,
                UserData.Password,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.LastNameRequired);
        }

        [Fact]
        public void Should_ComputeInitials_From_FirstAndLastName()
        {
            // Act
            User user = UserData.GetActiveUser();

            // Assert
            user.Initials.Should().Be($"{UserData.FirstName[0]}{UserData.LastName[0]}".ToUpperInvariant());
        }
    }

    public sealed class ChangePassword : BaseTest
    {
        [Fact]
        public void Should_ChangePassword_When_Called()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result result = user.ChangePassword(UserData.NewPassword, UserData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Should_UpdateUpdatedOnUtc_When_PasswordChanged()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            user.ChangePassword(UserData.NewPassword, UserData.UpdatedOnUtc);

            // Assert
            user.UpdatedOnUtc.Should().Be(UserData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RaiseUserPasswordChangedDomainEvent_When_PasswordChanged()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            user.ChangePassword(UserData.NewPassword, UserData.UpdatedOnUtc);

            // Assert
            UserPasswordChangedDomainEvent domainEvent = AssertDomainEventWasPublished<UserPasswordChangedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
        }
    }

    public sealed class Deactivate : BaseTest
    {
        [Fact]
        public void Should_DeactivateUser_When_Active()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result result = user.Deactivate(UserData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            user.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Should_UpdateUpdatedOnUtc_When_Deactivated()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            user.Deactivate(UserData.UpdatedOnUtc);

            // Assert
            user.UpdatedOnUtc.Should().Be(UserData.UpdatedOnUtc);
        }

        [Fact]
        public void Should_RaiseUserDeactivatedDomainEvent_When_Deactivated()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            user.Deactivate(UserData.UpdatedOnUtc);

            // Assert
            UserDeactivatedDomainEvent domainEvent = AssertDomainEventWasPublished<UserDeactivatedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void Should_Fail_When_UserIsAlreadyDeactivated()
        {
            // Arrange
            User user = UserData.GetInactiveUser();

            // Act
            Result result = user.Deactivate(UserData.UpdatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.AlreadyDeactivated);
        }
    }

    public sealed class IssueToken : BaseTest
    {
        [Fact]
        public void Should_IssueToken_When_DataIsValid()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.IsRevoked.Should().BeFalse();
        }

        [Fact]
        public void Should_AddTokenToCollection_When_Issued()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            user.Tokens.Should().ContainSingle();
        }

        [Fact]
        public void Should_RaiseUserTokenIssuedDomainEvent_When_Issued()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            UserTokenIssuedDomainEvent domainEvent = AssertDomainEventWasPublished<UserTokenIssuedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
            domainEvent.UserTokenId.Should().Be(result.Value.UserTokenId);
        }

        [Fact]
        public void Should_Fail_When_AccessTokenIsEmpty()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                string.Empty,
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.AccessTokenRequired);
        }

        [Fact]
        public void Should_Fail_When_AccessTokenIsWhitespace()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                "   ",
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.AccessTokenRequired);
        }

        [Fact]
        public void Should_Fail_When_RefreshTokenIsEmpty()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                string.Empty,
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.RefreshTokenRequired);
        }

        [Fact]
        public void Should_Fail_When_RefreshTokenIsWhitespace()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                "   ",
                UserData.AccessTokenExpiresOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.RefreshTokenRequired);
        }

        [Fact]
        public void Should_Fail_When_AccessTokenExpiryIsBeforeIssuedDate()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.CreatedOnUtc.AddMinutes(-1),
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.InvalidExpiration);
        }

        [Fact]
        public void Should_Fail_When_AccessTokenExpiryEqualsIssuedDate()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.CreatedOnUtc,
                UserData.RefreshTokenExpiresOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.InvalidExpiration);
        }

        [Fact]
        public void Should_Fail_When_RefreshTokenExpiryIsBeforeIssuedDate()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.CreatedOnUtc.AddMinutes(-1),
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.InvalidExpiration);
        }

        [Fact]
        public void Should_Fail_When_RefreshTokenExpiryEqualsIssuedDate()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result<UserToken> result = user.IssueToken(
                UserData.AccessToken,
                UserData.RefreshToken,
                UserData.AccessTokenExpiresOnUtc,
                UserData.CreatedOnUtc,
                UserData.CreatedOnUtc);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.InvalidExpiration);
        }
    }

    public sealed class RevokeToken : BaseTest
    {
        [Fact]
        public void Should_RevokeToken_When_TokenExists()
        {
            // Arrange
            User user = UserData.GetUserWithToken(out Guid tokenId);

            // Act
            Result result = user.RevokeToken(tokenId);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            user.Tokens.Single(t => t.UserTokenId == tokenId).IsRevoked.Should().BeTrue();
        }

        [Fact]
        public void Should_RaiseUserTokenRevokedDomainEvent_When_Revoked()
        {
            // Arrange
            User user = UserData.GetUserWithToken(out Guid tokenId);

            // Act
            user.RevokeToken(tokenId);

            // Assert
            UserTokenRevokedDomainEvent domainEvent = AssertDomainEventWasPublished<UserTokenRevokedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
            domainEvent.UserTokenId.Should().Be(tokenId);
        }

        [Fact]
        public void Should_Fail_When_TokenNotFound()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result result = user.RevokeToken(Guid.NewGuid());

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.NotFound);
        }

        [Fact]
        public void Should_Fail_When_TokenAlreadyRevoked()
        {
            // Arrange
            User user = UserData.GetUserWithToken(out Guid tokenId);
            user.RevokeToken(tokenId);

            // Act
            Result result = user.RevokeToken(tokenId);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserTokenErrors.AlreadyRevoked);
        }
    }

    public sealed class AssignRole : BaseTest
    {
        [Fact]
        public void Should_AssignRole_When_NotAlreadyAssigned()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result result = user.AssignRole(Role.Member);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            user.Roles.Should().ContainSingle(r => r.Name == Role.Member.Name);
        }

        [Fact]
        public void Should_RaiseUserRoleAssignedDomainEvent_When_Assigned()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            user.AssignRole(Role.Member);

            // Assert
            UserRoleAssignedDomainEvent domainEvent = AssertDomainEventWasPublished<UserRoleAssignedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
            domainEvent.RoleName.Should().Be(Role.Member.Name);
        }

        [Fact]
        public void Should_Fail_When_RoleAlreadyAssigned()
        {
            // Arrange
            User user = UserData.GetUserWithRole(Role.Member);

            // Act
            Result result = user.AssignRole(Role.Member);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.RoleAlreadyAssigned);
        }

        [Fact]
        public void Should_Throw_When_RoleIsNull()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Action act = () => user.AssignRole(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }

    public sealed class RemoveRole : BaseTest
    {
        [Fact]
        public void Should_RemoveRole_When_RoleIsAssigned()
        {
            // Arrange
            User user = UserData.GetUserWithRole(Role.Member);

            // Act
            Result result = user.RemoveRole(Role.Member);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            user.Roles.Should().BeEmpty();
        }

        [Fact]
        public void Should_RaiseUserRoleRemovedDomainEvent_When_Removed()
        {
            // Arrange
            User user = UserData.GetUserWithRole(Role.Member);

            // Act
            user.RemoveRole(Role.Member);

            // Assert
            UserRoleRemovedDomainEvent domainEvent = AssertDomainEventWasPublished<UserRoleRemovedDomainEvent>(user);
            domainEvent.UserId.Should().Be(user.Id);
            domainEvent.RoleName.Should().Be(Role.Member.Name);
        }

        [Fact]
        public void Should_Fail_When_RoleNotAssigned()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Result result = user.RemoveRole(Role.Member);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(UserErrors.RoleNotAssigned);
        }

        [Fact]
        public void Should_Throw_When_RoleIsNull()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Action act = () => user.RemoveRole(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }

    public sealed class VerifyPassword : BaseTest
    {
        private sealed class FakePasswordHasher(bool verifyResult) : IPasswordHasher
        {
            public string HashPassword(string password) => password;
            public bool VerifyHashedPassword(string hashedPassword, string providedPassword) => verifyResult;
            public void VerifyDummy(string providedPassword) { }
        }

        [Fact]
        public void Should_ReturnTrue_When_PasswordMatchesHash()
        {
            // Arrange
            User user = UserData.GetActiveUser();
            IPasswordHasher hasher = new FakePasswordHasher(true);

            // Act
            bool result = user.VerifyPassword(hasher, "correct-password");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Should_ReturnFalse_When_PasswordDoesNotMatchHash()
        {
            // Arrange
            User user = UserData.GetActiveUser();
            IPasswordHasher hasher = new FakePasswordHasher(false);

            // Act
            bool result = user.VerifyPassword(hasher, "wrong-password");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Should_Throw_When_PasswordHasherIsNull()
        {
            // Arrange
            User user = UserData.GetActiveUser();

            // Act
            Action act = () => user.VerifyPassword(null!, "any-password");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
