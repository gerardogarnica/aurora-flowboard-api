using Aurora.Flowboard.Domain.Abstractions;

namespace Aurora.Flowboard.Api.Responses;

internal static class ResultExtensions
{
    internal static TOut Match<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Result, TOut> onFailure)
    {
        return result.IsSuccessful
            ? onSuccess()
            : onFailure(result);
    }

    internal static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Result<TIn>, TOut> onFailure)
    {
        return result.IsSuccessful
            ? onSuccess(result.Value)
            : onFailure(result);
    }
}
