using System.Net;
using AstraSystemsRental.Base.Contracts;
using Microsoft.AspNetCore.Http;

namespace AstraSystemsRental.Base.Http;

public static class OperationResultExtensions
{
    public static IResult ToResult(this OperationResult operationResult, HttpContext? httpContext = null)
    {
        var traceId = operationResult.TraceId ?? httpContext?.TraceIdentifier;
        var payload = new ApiResponse(operationResult.Success, operationResult.Data, operationResult.Errors, traceId);

        return operationResult.StatusCode switch
        {
            HttpStatusCode.OK => Results.Ok(payload),
            HttpStatusCode.Created => Results.Json(payload, statusCode: StatusCodes.Status201Created),
            HttpStatusCode.NoContent => Results.NoContent(),
            HttpStatusCode.BadRequest => Results.Json(payload, statusCode: StatusCodes.Status400BadRequest),
            HttpStatusCode.Unauthorized => Results.Json(payload, statusCode: StatusCodes.Status401Unauthorized),
            HttpStatusCode.Forbidden => Results.Json(payload, statusCode: StatusCodes.Status403Forbidden),
            HttpStatusCode.NotFound => Results.Json(payload, statusCode: StatusCodes.Status404NotFound),
            HttpStatusCode.Conflict => Results.Json(payload, statusCode: StatusCodes.Status409Conflict),
            HttpStatusCode.InternalServerError => Results.Json(payload, statusCode: StatusCodes.Status500InternalServerError),
            _ => Results.Json(payload, statusCode: (int)operationResult.StatusCode)
        };
    }
}
