namespace VitalTrek.API.Shared.Application.Patterns;

/// <summary>
///     Executes the persistence part of a command and translates expected exceptions into <see cref="Result{TValue,TError}"/>
///     failures, so command services keep a single, consistent error handling strategy.
/// </summary>
public static class CommandExecutor
{
    /// <summary>
    ///     Runs a command action.
    /// </summary>
    /// <param name="action">The action that mutates and persists the aggregates.</param>
    /// <param name="mapException">Maps an expected exception to an error, or returns null for unexpected exceptions.</param>
    /// <param name="unexpectedError">The error returned for unexpected exceptions.</param>
    /// <param name="logger">The logger used to record failures.</param>
    /// <param name="operation">A short description of the operation for diagnostics.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <returns>The success value or the mapped error.</returns>
    public static async Task<Result<TValue, TError>> RunAsync<TValue, TError>(
        Func<Task<TValue>> action,
        Func<Exception, TError?> mapException,
        TError unexpectedError,
        ILogger logger,
        string operation,
        CancellationToken cancellationToken) where TError : struct, Enum
    {
        try
        {
            return new Result<TValue, TError>.Success(await action());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = mapException(ex);
            if (error is not null)
            {
                logger.LogWarning(ex, "Operation {Operation} rejected with {Error}", operation, error);
                return new Result<TValue, TError>.Failure(error.Value);
            }

            logger.LogError(ex, "Unexpected error during {Operation}", operation);
            return new Result<TValue, TError>.Failure(unexpectedError);
        }
    }
}
