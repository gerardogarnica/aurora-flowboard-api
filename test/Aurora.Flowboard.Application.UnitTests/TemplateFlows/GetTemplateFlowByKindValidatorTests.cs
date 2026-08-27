namespace Aurora.Flowboard.Application.UnitTests.TemplateFlows;

public sealed class GetTemplateFlowByKindValidatorTests
{
    private readonly GetTemplateFlowByKindValidator _validator = new();

    [Fact]
    public void Should_Pass_When_KindIsValidEnumValue()
    {
        GetTemplateFlowByKindQuery query = new(ProjectKind.Product);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_KindIsOutOfRange()
    {
        GetTemplateFlowByKindQuery query = new((ProjectKind)999);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
