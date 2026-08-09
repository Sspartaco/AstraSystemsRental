using System.Net;
using AstraSystemsRental.Base.Contracts;
using FluentAssertions;

namespace AstraSystemsRental.Base.Tests;

public class OperationResultTests
{
    [Fact(DisplayName = "Ok builds a success result with data and 200 status")]
    public void Ok_WithData_IsSuccess()
    {
        // Arrange
        var data = new { Id = 1 };

        // Act
        var result = OperationResult.Ok(data);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(data);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Fail builds an error result with 400 status by default")]
    public void Fail_WithMessage_IsBadRequest()
    {
        // Act
        var result = OperationResult.Fail("invalid");

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Errors.Should().ContainSingle().Which.Should().Be("invalid");
    }

    [Fact(DisplayName = "NotFound maps to 404")]
    public void NotFound_MapsTo404()
    {
        // Act
        var result = OperationResult.NotFound("missing");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Success.Should().BeFalse();
    }

    [Fact(DisplayName = "Conflict maps to 409")]
    public void Conflict_MapsTo409()
    {
        // Act
        var result = OperationResult.Conflict("duplicate");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Unauthorized maps to 401")]
    public void Unauthorized_MapsTo401()
    {
        // Act
        var result = OperationResult.Unauthorized();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Fail filters empty error strings")]
    public void Fail_WithEmptyStrings_AreFiltered()
    {
        // Act
        var result = OperationResult.Fail(["", "  ", "real"]);

        // Assert
        result.Errors.Should().ContainSingle().Which.Should().Be("real");
    }
}
