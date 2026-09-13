using System.Net;

namespace WebRequesterDll.Tests;

/// <summary>
///     Exercises the SSRF guard's address-classification logic directly (via InternalsVisibleTo) so it can be
///     tested without touching real DNS.
/// </summary>
public class DomainAuditorAddressGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.55.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")] // cloud metadata endpoint
    [InlineData("100.64.0.1")] // carrier-grade NAT
    [InlineData("0.0.0.0")]
    [InlineData("::1")] // IPv6 loopback
    [InlineData("fe80::1")] // IPv6 link-local
    [InlineData("fc00::1")] // IPv6 unique local
    public void IsDisallowedAddress_ReturnsTrue_ForPrivateLoopbackOrLinkLocalAddresses(string ip)
    {
        var address = IPAddress.Parse(ip);

        Assert.True(DomainAuditor.IsDisallowedAddress(address));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.15.255.255")] // just below the 172.16.0.0/12 private range
    [InlineData("172.32.0.1")] // just above the 172.16.0.0/12 private range
    [InlineData("2606:4700:4700::1111")] // public IPv6 (Cloudflare)
    public void IsDisallowedAddress_ReturnsFalse_ForPubliclyRoutableAddresses(string ip)
    {
        var address = IPAddress.Parse(ip);

        Assert.False(DomainAuditor.IsDisallowedAddress(address));
    }
}
