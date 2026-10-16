using System.Net;
using FullRes.Api.Security;

namespace FullRes.Tests.Security;

public class SsrfGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    public void RejectsPrivateAndReservedIpv4(string address)
    {
        Assert.False(SsrfGuard.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("93.184.216.34")]
    [InlineData("172.32.0.1")]
    public void AcceptsPublicIpv4(string address)
    {
        Assert.True(SsrfGuard.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:127.0.0.1")]
    public void RejectsPrivateIpv6(string address)
    {
        Assert.False(SsrfGuard.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("2001:4860:4860::8888")]
    public void AcceptsPublicIpv6(string address)
    {
        Assert.True(SsrfGuard.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("https://example.com/image.jpg")]
    [InlineData("http://example.com:80/image.jpg")]
    [InlineData("https://example.com:443/image.jpg")]
    public void AllowsHttpAndHttpsOnStandardPorts(string url)
    {
        SsrfGuard.ValidateUri(new Uri(url));
    }

    [Theory]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://example.com:8080/image.jpg")]
    [InlineData("http://localhost/image.jpg")]
    [InlineData("http://127.0.0.1/image.jpg")]
    [InlineData("http://192.168.0.10/image.jpg")]
    [InlineData("http://metadata.google.internal/latest/meta-data")]
    public void BlocksDangerousUris(string url)
    {
        Assert.Throws<HttpRequestException>(() => SsrfGuard.ValidateUri(new Uri(url)));
    }

    [Theory]
    [InlineData(80, true)]
    [InlineData(443, true)]
    [InlineData(8080, false)]
    [InlineData(22, false)]
    public void RestrictsPorts(int port, bool allowed)
    {
        Assert.Equal(allowed, SsrfGuard.IsPortAllowed(port));
    }
}
