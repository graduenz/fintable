using System.Net;

namespace Fintable.Tests.Api;

public class ApiSetupTests : BaseControllerTests
{
    [Fact]
    public async Task UnknownEndpoint_ReturnsNotFound_WithProblemDetails()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/__route-that-will-never-exist__");
        request.Headers.Add("Accept", "application/problem+json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
