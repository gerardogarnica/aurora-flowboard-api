using Aurora.Flowboard.Application.Abstractions.Validations;
using FluentValidation;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Validations;

public sealed class ColorRuleExtensionsTests
{
    private const string ValidColor = "#3B82F6";

    private readonly ColorHolderValidator _validator = new();

    [Fact]
    public void Should_BeValid_When_ColorIsWellFormed()
    {
        ValidationResult result = _validator.Validate(new ColorHolder(ValidColor));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_BeValid_When_ColorHasMaxLength()
    {
        ValidationResult result = _validator.Validate(new ColorHolder(new string('A', Color.MaxLength)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_BeInvalid_When_ColorExceedsMaxLength()
    {
        ValidationResult result = _validator.Validate(new ColorHolder(new string('A', Color.MaxLength + 1)));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_BeInvalid_When_ColorIsMissing(string? color)
    {
        ValidationResult result = _validator.Validate(new ColorHolder(color!));

        result.IsValid.Should().BeFalse();
    }

    private sealed record ColorHolder(string Color);

    private sealed class ColorHolderValidator : AbstractValidator<ColorHolder>
    {
        public ColorHolderValidator()
        {
            RuleFor(x => x.Color)
                .MustBeValidColor();
        }
    }
}
