using Core.Shared.Errors;

namespace Core.Shared.Results;

public static class ResultExtensions
{
    public static Result<TOut> Map<TOut>(this Result result, Func<TOut> func) =>
        result.IsSuccess ? func() : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Map<TOut>(this Task<Result> resultTask, Func<TOut> func) =>
        (await resultTask).Map(func);

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func) =>
        result.IsSuccess ? func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> func) =>
        (await resultTask).Map(func);

    public static Result<TIn> MapFailure<TIn>(this Result<TIn> result, Func<Error> func) =>
        result.IsSuccess ? result : Result.Failure<TIn>(func());

    public static async Task<Result<TIn>> MapFailure<TIn>(this Task<Result<TIn>> resultTask, Func<Error> func) =>
        (await resultTask).MapFailure(func);

    public static Result Bind<TIn>(this Result<TIn> result, Func<TIn, Result> func) =>
        result.IsSuccess ? func(result.Value) : Result.Failure(result.Error);

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func) =>
        result.IsSuccess ? func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func) =>
        result.IsSuccess ? await func(result.Value) : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Bind<TOut>(this Result result, Func<Task<Result<TOut>>> func) =>
        result.IsSuccess ? await func() : Result.Failure<TOut>(result.Error);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Result<TOut>> func) =>
        (await resultTask).Bind(func);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> func) =>
        await (await resultTask).Bind(func);

    public static async Task<Result> Bind<TIn>(this Result<TIn> result, Func<TIn, Task<Result>> func) =>
        result.IsSuccess ? await func(result.Value) : Result.Failure(result.Error);

    public static async Task<Result> Bind<TIn>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result>> func) =>
        await (await resultTask).Bind(func);

    public static async Task<Result> Tap(this Result result, Func<Task> func)
    {
        if (result.IsSuccess)
            await func();

        return result;
    }

    public static async Task<Result> Tap(this Task<Result> resultTask, Func<Task> func) => await (await resultTask).Tap(func);

    public static Result<TIn> Tap<TIn>(this Result<TIn> result, Action<TIn> action)
    {
        if (result.IsSuccess)
            action(result.Value);

        return result;
    }

    public static async Task<Result<TIn>> Tap<TIn>(this Task<Result<TIn>> resultTask, Func<Task> func) => await (await resultTask).Tap(func);

    public static async Task<Result<TIn>> Tap<TIn>(this Task<Result<TIn>> resultTask, Action<TIn> action) => (await resultTask).Tap(action);

    public static async Task<Result<TIn>> Tap<TIn>(this Result<TIn> result, Func<Task> func)
    {
        if (result.IsSuccess)
            await func();

        return result;
    }

    public static async Task<Result> OnFailure(this Task<Result> resultTask, Action<Error> action)
    {
        Result result = await resultTask;

        if (!result.IsSuccess)
            action(result.Error);

        return result;
    }

    public static async Task<Result<TIn>> OnFailure<TIn>(this Task<Result<TIn>> resultTask, Action<Error> action)
    {
        Result<TIn> result = await resultTask;

        if (!result.IsSuccess)
            action(result.Error);

        return result;
    }

    public static Result<TIn> Filter<TIn>(this Result<TIn> result, Func<TIn, bool> predicate)
    {
        if (!result.IsSuccess)
            return result;

        return predicate(result.Value) ? result : Result.Failure<TIn>(Error.ConditionNotMet);
    }
}