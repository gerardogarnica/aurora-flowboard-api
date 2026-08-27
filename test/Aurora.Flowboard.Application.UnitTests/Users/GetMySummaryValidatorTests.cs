using Aurora.Flowboard.Application.Users.GetMySummary;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class GetMySummaryValidatorTests
{
    private readonly GetMySummaryValidator _validator = new();

    [Fact]
    public void Should_Fail_When_UserIdIsEmpty()
    {
        // Arrange
        var query = new GetMySummaryQuery(Guid.Empty);

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "User ID is required");
    }

    [Fact]
    public void Should_Pass_When_UserIdIsProvided()
    {
        // Arrange
        var query = new GetMySummaryQuery(Guid.NewGuid());

        // Act
        FluentValidation.Results.ValidationResult result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
