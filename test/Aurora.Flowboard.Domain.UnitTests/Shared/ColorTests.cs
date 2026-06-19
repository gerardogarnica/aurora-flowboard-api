namespace Aurora.Flowboard.Domain.UnitTests.Shared;

public sealed class ColorTests
{
    [Fact]
    public void Should_CreateColor_When_ValueIsValid()
    {
        Result<Color> result = Color.Create("white");

        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("white");
    }

    [Fact]
    public void Should_TrimValue_When_Creating()
    {
        Result<Color> result = Color.Create("  white  ");

        result.IsSuccessful.Should().BeTrue();
        result.Value.Value.Should().Be("white");
    }

    [Fact]
    public void Should_Fail_When_ValueIsEmpty()
    {
        Result<Color> result = Color.Create(string.Empty);

        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ColorErrors.ColorRequired);
    }

    [Fact]
    public void Should_Fail_When_ValueIsWhitespace()
    {
        Result<Color> result = Color.Create("   ");

        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ColorErrors.ColorRequired);
    }

    [Fact]
    public void Should_Fail_When_ValueExceedsMaxLength()
    {
        string longColor = new('A', Color.MaxLength + 1);

        Result<Color> result = Color.Create(longColor);

        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(ColorErrors.ColorTooLong);
    }

    [Fact]
    public void Should_Pass_When_ValueEqualsMaxLength()
    {
        string exactColor = new('A', Color.MaxLength);

        Result<Color> result = Color.Create(exactColor);

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Should_ConvertToString_When_ImplicitOperatorUsed()
    {
        Color color = Color.Create("blue").Value;

        string value = color;

        value.Should().Be("blue");
    }

    [Fact]
    public void Should_ReturnValue_When_ToStringCalled()
    {
        Color color = Color.Create("red").Value;

        color.ToString().Should().Be("red");
    }

    [Fact]
    public void Should_BeEqual_When_ValuesMatch()
    {
        Color colorA = Color.Create("green").Value;
        Color colorB = Color.Create("green").Value;

        colorA.Should().Be(colorB);
    }

    [Fact]
    public void Should_NotBeEqual_When_ValuesDiffer()
    {
        Color colorA = Color.Create("green").Value;
        Color colorB = Color.Create("blue").Value;

        colorA.Should().NotBe(colorB);
    }
}
