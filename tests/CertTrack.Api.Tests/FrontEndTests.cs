using System.Net;

namespace CertTrack.Api.Tests;

public class FrontEndTests : ApiTestBase
{
    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/app.js", "text/javascript")]
    [InlineData("/styles.css", "text/css")]
    public async Task StaticFile_IsServed(string url, string contentType)
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/objectives")]
    public async Task Response_HasSecurityHeaders(string url)
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Contains("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }
}
