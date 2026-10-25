using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InvestmentLab.BuildingBlocks.Tests.Http;

public sealed class ResultHttpExtensionsTests
{
    [Fact]
    public void Success_without_value_is_no_content()
    {
        Result.Success().ToHttpResult().ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void Success_with_value_is_ok_with_value()
    {
        Result<int> result = 42;

        result.ToHttpResult().ShouldBeOfType<Ok<int>>().Value.ShouldBe(42);
    }

    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    public void Failure_is_problem_details_with_status_by_kind(ErrorKind kind, int expectedStatus)
    {
        Result result = new Error("report.not_open", "Report is closed", kind);

        var problem = result.ToHttpResult().ShouldBeOfType<ProblemHttpResult>();

        problem.StatusCode.ShouldBe(expectedStatus);
        problem.ProblemDetails.Detail.ShouldBe("Report is closed");
        problem.ProblemDetails.Extensions["code"].ShouldBe("report.not_open");
    }

    [Fact]
    public void Failed_typed_result_is_problem_details()
    {
        Result<int> result = Error.NotFound("x.not_found", "Missing");

        result.ToHttpResult().ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
