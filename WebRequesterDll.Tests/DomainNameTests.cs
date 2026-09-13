namespace WebRequesterDll.Tests;

public class DomainNameTests
{
    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("https://example.com/path", "example.com")]
    [InlineData("http://www.example.com", "www.example.com")]
    [InlineData("//example.com/path", "example.com")]
    [InlineData("EXAMPLE.COM", "example.com")] // Uri.Host normalizes casing
    public void TryParse_ExtractsHost_FromVariousInputForms(string input, string expectedHost)
    {
        var success = DomainName.TryParse(input, out var domain);

        Assert.True(success);
        Assert.Equal(expectedHost, domain);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_Fails_ForEmptyOrWhitespaceInput(string? input)
    {
        var success = DomainName.TryParse(input!, out var domain);

        Assert.False(success);
        Assert.Equal(string.Empty, domain);
    }

    [Fact]
    public void TryParse_Fails_ForSchemeOnlyGarbage()
    {
        var success = DomainName.TryParse("https://", out var domain);

        Assert.False(success);
        Assert.Equal(string.Empty, domain);
    }
}
