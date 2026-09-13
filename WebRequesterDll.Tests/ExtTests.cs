namespace WebRequesterDll.Tests;

public class ExtTests
{
    [Theory]
    [InlineData("200", 200)]
    [InlineData("404", 404)]
    [InlineData("NotFound", 404)]
    [InlineData("OK", 200)]
    public void ToHttpStatusCode_ParsesValidNumericOrEnumNameStrings(string input, int expected)
    {
        Assert.Equal(expected, input.ToHttpStatusCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-status")]
    [InlineData("999999")]
    public void ToHttpStatusCode_ReturnsNegativeOne_ForInvalidInput(string input)
    {
        Assert.Equal(-1, input.ToHttpStatusCode());
    }

    [Theory]
    [InlineData(200, "OK")]
    [InlineData(404, "NotFound")]
    public void ToHttpStatusCode_ConvertsIntToEnumName(int input, string expected)
    {
        Assert.Equal(expected, input.ToHttpStatusCode());
    }

    [Fact]
    public void ToHttpStatusCode_ReturnsUnknown_ForUndefinedIntValue()
    {
        Assert.Equal("unknown", 999999.ToHttpStatusCode());
    }
}
