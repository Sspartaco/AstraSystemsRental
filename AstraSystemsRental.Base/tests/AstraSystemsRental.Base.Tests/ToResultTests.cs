using System.Net;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AstraSystemsRental.Base.Tests;

public class ToResultTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.AspNetCore.Http.IProblemDetailsService, StubProblemDetailsService>();
        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    [Theory(DisplayName = "ToResult maps each status code to the matching HTTP response")]
    [InlineData(HttpStatusCode.OK, StatusCodes.Status200OK)]
    [InlineData(HttpStatusCode.Created, StatusCodes.Status201Created)]
    [InlineData(HttpStatusCode.NoContent, StatusCodes.Status204NoContent)]
    [InlineData(HttpStatusCode.BadRequest, StatusCodes.Status400BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(HttpStatusCode.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, StatusCodes.Status500InternalServerError)]
    public async Task ToResult_MapsStatusCode(HttpStatusCode input, int expected)
    {
        // Arrange
        var operationResult = input switch
        {
            HttpStatusCode.OK => OperationResult.Ok("ok"),
            HttpStatusCode.Created => OperationResult.Created("created"),
            HttpStatusCode.NoContent => OperationResult.NoContent(),
            _ => OperationResult.Fail("err", input)
        };
        var context = CreateHttpContext();

        // Act
        var result = operationResult.ToResult(context);
        await result.ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(expected);
    }

    private sealed class StubProblemDetailsService : Microsoft.AspNetCore.Http.IProblemDetailsService
    {
        public ValueTask WriteAsync(Microsoft.AspNetCore.Http.ProblemDetailsContext context) => ValueTask.CompletedTask;
        public bool TryWrite(Microsoft.AspNetCore.Http.ProblemDetailsContext context) => true;
    }
}
