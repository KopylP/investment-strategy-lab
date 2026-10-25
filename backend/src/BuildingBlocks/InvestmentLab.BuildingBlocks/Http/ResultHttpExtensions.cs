using InvestmentLab.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace InvestmentLab.BuildingBlocks.Http;

/// <summary>Maps domain results to HTTP: success → 200/204, error → RFC 9457 problem details.</summary>
public static class ResultHttpExtensions
{
    extension(Result result)
    {
        public IResult ToHttpResult() => result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    extension<T>(Result<T> result)
    {
        public IResult ToHttpResult() => result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    extension(Error error)
    {
        public IResult ToProblem() =>
            TypedResults.Problem(
                detail: error.Message,
                statusCode: error.Kind switch
                {
                    ErrorKind.Validation => StatusCodes.Status400BadRequest,
                    ErrorKind.NotFound => StatusCodes.Status404NotFound,
                    ErrorKind.Conflict => StatusCodes.Status409Conflict,
                    ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status500InternalServerError,
                },
                extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
