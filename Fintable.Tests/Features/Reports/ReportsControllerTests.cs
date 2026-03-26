using System.Net;

namespace Fintable.Tests.Features.Reports;

public class ReportsControllerTests : BaseControllerTests
{
    [Fact]
    public async Task Fintable_ReturnsInternalServerError_WithProblemDetails()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/reports/fintable");
        request.Headers.Add("Accept", "application/problem+json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
