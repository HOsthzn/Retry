using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a retry strategy for determining the delay between retry attempts.
/// </summary>
public interface IRetryStrategy
{
    /// <summary>
    /// Gets the delay for the specified retry attempt.
    /// </summary>
    /// <param name="retryAttempt">The current retry attempt (1-based).</param>
    /// <returns>The delay duration for the retry attempt.</returns>
    TimeSpan GetNextDelay(int retryAttempt);
}

/// <summary>
/// Represents a retry strategy that uses a fixed delay interval for each retry attempt.
/// </summary>
public sealed class FixedIntervalStrategy : IRetryStrategy
{
    private readonly TimeSpan _delay;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedIntervalStrategy"/> class.
    /// </summary>
    /// <param name="delay">The fixed time interval between retries.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delay"/> is negative.</exception>
    public FixedIntervalStrategy(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
        _delay = delay;
    }

    public TimeSpan GetNextDelay(int retryAttempt) => _delay;
}

/// <summary>
/// Represents an exponential back-off strategy for retrying operations.
/// </summary>
public sealed class ExponentialBackOffStrategy : IRetryStrategy
{
    private readonly double _factor;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackOffStrategy"/> class.
    /// </summary>
    /// <param name="initialDelay">The initial delay before the first retry.</param>
    /// <param name="maxDelay">The maximum delay between retries.</param>
    /// <param name="factor">The factor by which the delay increases after each retry (default is 2).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelay"/> is negative.</exception>
    public ExponentialBackOffStrategy(TimeSpan initialDelay, TimeSpan maxDelay, double factor = 2)
    {
        if (initialDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay cannot be negative.");
        _initialDelay = initialDelay;
        _factor = factor;
        _maxDelay = maxDelay;
    }

    public TimeSpan GetNextDelay(int retryAttempt)
    {
        var delayTicks = (long)(_initialDelay.Ticks * Math.Pow(_factor, retryAttempt - 1));
        return TimeSpan.FromTicks(delayTicks) > _maxDelay ? _maxDelay : TimeSpan.FromTicks(delayTicks);
    }
}

/// <summary>
/// Represents a retry strategy that implements exponential back-off with jitter.
/// </summary>
public sealed class ExponentialBackOffWithJitterStrategy : IRetryStrategy
{
    private readonly double _factor;
    private readonly TimeSpan _initialDelay;
    private readonly double _jitterFactor;
    private readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackOffWithJitterStrategy"/> class.
    /// </summary>
    /// <param name="initialDelay">The initial delay before the first retry.</param>
    /// <param name="factor">The multiplier for delay increase per retry (default is 2).</param>
    /// <param name="jitterFactor">The factor for applying jitter (default is 0.2).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelay"/> is negative.</exception>
    public ExponentialBackOffWithJitterStrategy(TimeSpan initialDelay, double factor = 2, double jitterFactor = 0.2)
    {
        if (initialDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay cannot be negative.");
        _initialDelay = initialDelay;
        _factor = factor;
        _jitterFactor = jitterFactor;
    }

    public TimeSpan GetNextDelay(int retryAttempt)
    {
        var delayTicks = (long)(_initialDelay.Ticks * Math.Pow(_factor, retryAttempt - 1));
        var delay = TimeSpan.FromTicks(delayTicks);
        var jitter = TimeSpan.FromMilliseconds(Math.Abs(delay.TotalMilliseconds * _jitterFactor * (NextDouble() * 2 - 1)));
        return delay + jitter;
    }

    private double NextDouble()
    {
        byte[] bytes = new byte[sizeof(double)];
        _random.GetBytes(bytes);
        return BitConverter.ToUInt64(bytes, 0) / (double)(1UL << 53);
    }
}

/// <summary>
/// Provides methods for retrying actions with configurable retry strategies and conditions.
/// </summary>
public static class Retry
{
    /// <summary>
    /// Executes an asynchronous action with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="retryCount">The maximum number of retry attempts.</param>
    /// <param name="retryStrategy">The strategy for retry delays. Defaults to <see cref="FixedIntervalStrategy"/> with 1-second delay.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <param name="shouldRetryOnExceptions">Predicates to determine if an exception should trigger a retry.</param>
    /// <param name="shouldRetryOnResults">Predicates to determine if a result should trigger a retry.</param>
    /// <param name="retriableExceptions">Exception types that are retriable.</param>
    /// <returns>A task representing the result of the action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retryCount"/> is negative.</exception>
    /// <exception cref="AggregateException">Thrown when all retries fail.</exception>
    public static async Task<TResult> DoAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        int retryCount,
        IRetryStrategy? retryStrategy = null,
        CancellationToken cancellationToken = default,
        IEnumerable<Func<Exception, bool>>? shouldRetryOnExceptions = null,
        IEnumerable<Func<TResult, bool>>? shouldRetryOnResults = null,
        ReadOnlySpan<Type> retriableExceptions = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (retryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count cannot be negative.");

        retryStrategy ??= new FixedIntervalStrategy(TimeSpan.FromSeconds(1));
        return await RetryActionAsync(action, retryStrategy, retryCount, cancellationToken, shouldRetryOnExceptions, shouldRetryOnResults, retriableExceptions);
    }

    private static async Task<TResult> RetryActionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        IRetryStrategy retryStrategy,
        int retryCount,
        CancellationToken cancellationToken,
        IEnumerable<Func<Exception, bool>>? shouldRetryOnExceptions,
        IEnumerable<Func<TResult, bool>>? shouldRetryOnResults,
        ReadOnlySpan<Type> retriableExceptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<Exception> exceptions = [];

        for (int retry = 0; retry <= retryCount; retry++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (retry > 0)
            {
                var delay = retryStrategy.GetNextDelay(retry);
                if (delay < TimeSpan.Zero)
                    throw new InvalidOperationException("Retry delay cannot be negative.");
                await Task.Delay(delay, cancellationToken);
            }

            try
            {
                var result = await action(cancellationToken);
                if (shouldRetryOnResults?.Any(predicate => predicate(result)) == true)
                {
                    exceptions.Add(new Exception("Unexpected result"));
                    continue;
                }
                return result;
            }
            catch (Exception ex) when (retriableExceptions.IsEmpty || retriableExceptions.Contains(ex.GetType()))
            {
                if (shouldRetryOnExceptions?.Any(predicate => predicate(ex)) != true)
                    throw;
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }

    /// <summary>
    /// Executes a synchronous action with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="retryCount">The maximum number of retry attempts.</param>
    /// <param name="retryStrategy">The strategy for retry delays. Defaults to <see cref="FixedIntervalStrategy"/> with 1-second delay.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <param name="shouldRetryOnExceptions">Predicates to determine if an exception should trigger a retry.</param>
    /// <param name="shouldRetryOnResults">Predicates to determine if a result should trigger a retry.</param>
    /// <param name="retriableExceptions">Exception types that are retriable.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retryCount"/> is negative.</exception>
    /// <exception cref="AggregateException">Thrown when all retries fail.</exception>
    public static TResult Do<TResult>(
        Func<TResult> action,
        int retryCount,
        IRetryStrategy? retryStrategy = null,
        CancellationToken cancellationToken = default,
        IEnumerable<Func<Exception, bool>>? shouldRetryOnExceptions = null,
        IEnumerable<Func<TResult, bool>>? shouldRetryOnResults = null,
        ReadOnlySpan<Type> retriableExceptions = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (retryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count cannot be negative.");

        retryStrategy ??= new FixedIntervalStrategy(TimeSpan.FromSeconds(1));
        return RetryAction(action, retryStrategy, retryCount, cancellationToken, shouldRetryOnExceptions, shouldRetryOnResults, retriableExceptions);
    }

    private static TResult RetryAction<TResult>(
        Func<TResult> action,
        IRetryStrategy retryStrategy,
        int retryCount,
        CancellationToken cancellationToken,
        IEnumerable<Func<Exception, bool>>? shouldRetryOnExceptions,
        IEnumerable<Func<TResult, bool>>? shouldRetryOnResults,
        ReadOnlySpan<Type> retriableExceptions)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<Exception> exceptions = [];

        for (int retry = 0; retry <= retryCount; retry++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (retry > 0)
            {
                var delay = retryStrategy.GetNextDelay(retry);
                if (delay < TimeSpan.Zero)
                    throw new InvalidOperationException("Retry delay cannot be negative.");
                Task.Delay(delay, cancellationToken).Wait(cancellationToken);
            }

            try
            {
                var result = action();
                if (shouldRetryOnResults?.Any(predicate => predicate(result)) == true)
                {
                    exceptions.Add(new Exception("Unexpected result"));
                    continue;
                }
                return result;
            }
            catch (Exception ex) when (retriableExceptions.IsEmpty || retriableExceptions.Contains(ex.GetType()))
            {
                if (shouldRetryOnExceptions?.Any(predicate => predicate(ex)) != true)
                    throw;
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }
}
