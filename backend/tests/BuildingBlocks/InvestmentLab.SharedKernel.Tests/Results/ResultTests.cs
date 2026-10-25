namespace InvestmentLab.SharedKernel.Tests;

public sealed class ResultTests
{
    private static readonly Error SomeError = Error.Validation("test.invalid", "Invalid value");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Error_converts_to_failed_result()
    {
        Result result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Reading_error_of_successful_result_is_a_programming_error()
    {
        Should.Throw<InvalidOperationException>(() => Result.Success().Error);
    }

    [Fact]
    public void Value_converts_to_successful_typed_result()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Error_converts_to_failed_typed_result()
    {
        Result<int> result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Reading_value_of_failed_result_is_a_programming_error()
    {
        Result<int> result = SomeError;

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Error_factories_set_kind()
    {
        Error.Validation("a", "b").Kind.ShouldBe(ErrorKind.Validation);
        Error.NotFound("a", "b").Kind.ShouldBe(ErrorKind.NotFound);
        Error.Conflict("a", "b").Kind.ShouldBe(ErrorKind.Conflict);
        Error.Forbidden("a", "b").Kind.ShouldBe(ErrorKind.Forbidden);
    }
}
