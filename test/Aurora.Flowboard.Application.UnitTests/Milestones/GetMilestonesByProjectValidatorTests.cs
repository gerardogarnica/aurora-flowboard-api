namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class GetMilestonesByProjectValidatorTests
{
    private readonly GetMilestonesByProjectValidator _validator;

    public GetMilestonesByProjectValidatorTests()
    {
        _validator = new GetMilestonesByProjectValidator();
    }

    [Fact]
    public void Should_Pass_When_ProjectIdIsProvided()
    {
        var query = new GetMilestonesByProjectQuery(Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        var query = new GetMilestonesByProjectQuery(Guid.Empty);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
