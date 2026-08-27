namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class GetComponentsByProjectValidatorTests
{
    private readonly GetComponentsByProjectValidator _validator;

    public GetComponentsByProjectValidatorTests()
    {
        _validator = new GetComponentsByProjectValidator();
    }

    [Fact]
    public void Should_Pass_When_ProjectIdIsProvided()
    {
        var query = new GetComponentsByProjectQuery(Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        var query = new GetComponentsByProjectQuery(Guid.Empty);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
