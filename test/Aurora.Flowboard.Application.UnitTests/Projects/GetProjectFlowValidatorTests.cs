namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectFlowValidatorTests
{
    private readonly GetProjectFlowValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ProjectIdIsProvided()
    {
        GetProjectFlowQuery query = new(Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        GetProjectFlowQuery query = new(Guid.Empty);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
