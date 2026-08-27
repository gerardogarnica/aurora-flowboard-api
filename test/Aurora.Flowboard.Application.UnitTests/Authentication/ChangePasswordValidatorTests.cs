using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.ChangePassword;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator;

    public ChangePasswordValidatorTests()
    {
        _validator = new ChangePasswordValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_CurrentPasswordIsEmpty()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(currentPassword: string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewPasswordIsTooShort()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(newPassword: "Sh0rt!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewPasswordExceedsMaxLength()
    {
        string longPassword = $"Aa1!{new string('a', 128)}";
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(newPassword: longPassword);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewPasswordHasNoUppercaseLetter()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(newPassword: "n3wp@ssword!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewPasswordHasNoLowercaseLetter()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(newPassword: "N3WP@SSWORD!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewPasswordHasNoDigit()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(newPassword: "NewP@ssword!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewPasswordHasNoSpecialCharacter()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(newPassword: "N3wPassword");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_FailWithNewPasswordMustDifferErrorCode_When_NewPasswordEqualsCurrentPassword()
    {
        ChangePasswordCommand command = ChangePasswordCommandData.GetCommand(
            newPassword: ChangePasswordCommandData.CurrentPlainPassword);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == AuthenticationErrors.NewPasswordMustDiffer.Code);
    }
}
