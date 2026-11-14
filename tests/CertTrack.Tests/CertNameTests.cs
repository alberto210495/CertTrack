using CertTrack.Core;

namespace CertTrack.Tests;

public class CertNameTests
{
    [Theory]
    [InlineData("a+", "A+")]
    [InlineData("A+", "A+")]
    [InlineData("NETWORK+", "Network+")]
    public void Resolve_ReturnsTheSpellingInUse(string typed, string expected)
    {
        var cert = CertName.Resolve(["A+", "Network+"], typed);

        Assert.Equal(expected, cert);
    }

    [Fact]
    public void Resolve_ReturnsANewCertAsTyped()
    {
        var cert = CertName.Resolve(["A+"], "Security+");

        Assert.Equal("Security+", cert);
    }

    [Fact]
    public void Resolve_ReturnsTheCertAsTyped_WhenNoneAreInUse()
    {
        var cert = CertName.Resolve([], "a+");

        Assert.Equal("a+", cert);
    }
}
