/// <summary>
/// Represents a retry strategy for determining the delay between retry attempts.
/// </summary>
public interface IRetryStrategy
{
    TimeSpan GetNextDelay( int retryAttempt );
}

/// <summary>
/// Represents a retry strategy that uses a fixed delay interval for each retry attempt.
/// </summary>
public class FixedIntervalStrategy: IRetryStrategy
{
    /// <summary>
    /// Represents the delay duration used for a specific task or operation.
    /// </summary>
    private readonly TimeSpan _delay;

    /// <summary>
    /// Represents a strategy for executing code at fixed intervals.
    /// </summary>
    /// <param name="delay">The time interval between each execution.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the delay is negative.</exception>
    public FixedIntervalStrategy( TimeSpan delay )
    {
        if( delay < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( nameof( delay ), "Delay can't be negative" );
        _delay = delay;
    }

    /// <summary>
    /// Retrieves the next delay for a retry attempt.
    /// </summary>
    /// <param name="retryAttempt">The number of attempts made for the task.</param>
    /// <returns>The TimeSpan representing the delay for the next retry attempt.</returns>
    public TimeSpan GetNextDelay( int retryAttempt ) { return _delay; }
}

/// <summary>
/// Represents an exponential back-off strategy for retrying operations.
/// </summary>
public class ExponentialBackOffStrategy: IRetryStrategy
{
    /// <summary>
    /// The factor value used for calculations.
    /// </summary>
    private readonly double _factor;

    /// <summary>
    /// Represents the initial delay for a specific operation.
    /// </summary>
    private readonly TimeSpan _initialDelay;

    /// <summary>
    /// Represents the maximum delay value for a specific operation.
    /// </summary>
    private readonly TimeSpan _maxDelay;

    /// <summary>
    /// Initializes a new instance of the ExponentialBackOffStrategy class.
    /// </summary>
    /// <param name="initialDelay">The initial delay before the first retry.</param>
    /// <param name="maxDelay">The maximum delay between retries.</param>
    /// <param name="factor">The factor by which the delay should be multiplied after each retry (optional).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if initialDelay is negative.</exception>
    public ExponentialBackOffStrategy( TimeSpan initialDelay, TimeSpan maxDelay, double factor = 2 )
    {
        if( initialDelay < TimeSpan.Zero )
            throw new ArgumentOutOfRangeException( nameof( initialDelay ), "InitialDelay can't be negative" );
        _initialDelay = initialDelay;
        _factor = factor;
        _maxDelay = maxDelay;
    }

    /// <summary>
    /// Calculate the next delay based on the retry attempt.
    /// </summary>
    /// <param name="retryAttempt">The current retry attempt.</param>
    /// <returns>The calculated next delay as a TimeSpan.</returns>
    public TimeSpan GetNextDelay( int retryAttempt )
    {
        TimeSpan delay = TimeSpan.FromTicks(
            Convert.ToInt64( _initialDelay.Ticks * Math.Pow( _factor, retryAttempt - 1 ) ) );
        return delay > _maxDelay ? _maxDelay : delay;
    }
}

/// <summary>
/// Represents a retry strategy that implements exponential back-off with jitter algorithm.
/// </summary>
public class ExponentialBackOffWithJitterStrategy: IRetryStrategy
{
    /// <summary>
    /// The factor variable is a private double that represents a multiplication factor.
    /// </summary>
    private readonly double _factor;

    /// <summary>
    /// Represents the initial delay for a process or operation.
    /// </summary>
    private readonly TimeSpan _initialDelay;

    /// <summary>
    /// Represents the jitter factor that is used in a certain calculation.
    /// </summary>
    private readonly double _jitterFactor;

    /// <summary>
    /// Represents a random number generator.
    /// </summary>
    private readonly System.Security.Cryptography.RandomNumberGenerator _random
        = System.Security.Cryptography.RandomNumberGenerator.Create( );

    /// <summary>
    /// Initializes a new instance of the ExponentialBackOffWithJitterStrategy class.
    /// </summary>
    /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
    /// <param name="factor">The multiplier used to increase the delay for each subsequent retry attempt (default is 2).</param>
    /// <param name="jitterFactor">The factor used to apply jitter to the delay (default is 0.2).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the initialDelay is a negative value.</exception>
    public ExponentialBackOffWithJitterStrategy( TimeSpan initialDelay, double factor = 2, double jitterFactor = 0.2 )
    {
        if( initialDelay < TimeSpan.Zero )
            throw new ArgumentOutOfRangeException( nameof( initialDelay ), "InitialDelay can't be negative" );
        _initialDelay = initialDelay;
        _factor = factor;
        _jitterFactor = jitterFactor;
    }

    /// <summary>
    /// Calculates the next delay based on the retry attempt.
    /// </summary>
    /// <param name="retryAttempt">The retry attempt count.</param>
    /// <returns>The calculated delay as a TimeSpan.</returns>
    /// <remarks>
    /// The delay is calculated using the formula:
    /// delay = initialDelay * Math.Pow(factor, retryAttempt - 1)
    /// The delay is then adjusted using jitter to introduce randomness:
    /// jitter = delay * jitterFactor * (random double between -1 and 1)
    /// The final delay is the sum of the calculated delay and jitter.
    /// </remarks>
    public TimeSpan GetNextDelay( int retryAttempt )
    {
        TimeSpan delay = TimeSpan.FromTicks(
            Convert.ToInt64( _initialDelay.Ticks * Math.Pow( _factor, retryAttempt - 1 ) ) );
        TimeSpan jitter = TimeSpan.FromMilliseconds(
            Math.Abs( delay.TotalMilliseconds * _jitterFactor * ( NextDouble( ) * 2 - 1 ) ) );
        return delay + jitter;
    }

    /// <summary>
    /// Generates a random double value between 0.0 and 1.0 (inclusive).
    /// </summary>
    /// <returns>
    /// A random double value between 0.0 and 1.0 (inclusive).
    /// </returns>
    private double NextDouble( )
    {
        byte[ ] bytes = new byte[ sizeof( double ) ];
        _random.GetBytes( bytes );
        ulong ul = BitConverter.ToUInt64( bytes, 0 ) / ( 1 << 11 );
        return ul / ( double )( 1UL << 53 );
    }
}

/// <summary>
/// Provides methods for retrying actions and functions with optional retry strategies and retry conditions.
/// </summary>
public static class Retry
{
    /// <summary>
    /// Invokes the given asynchronous action with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the asynchronous action.</typeparam>
    /// <param name="action">The asynchronous action to invoke.</param>
    /// <param name="retryCount">The number of times to retry the action if it fails.</param>
    /// <param name="retryStrategy">The strategy for determining the delay between retries. If not specified, a <see cref="FixedIntervalStrategy"/> with a delay of 1 second will be used.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <param name="shouldRetryOnExceptions">A collection of predicate functions to determine if a specific exception should be retried. If not specified, no exceptions will be retried.</param>
    /// <param name="shouldRetryOnResults">A collection of predicate functions to determine if a specific result should be retried. If not specified, no results will be retried.</param>
    /// <param name="retriableExceptions">An array of types representing the exceptions that should be retried. If not specified, all exceptions will be retried.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task< TResult > DoAsync< TResult >(
        Func< CancellationToken, Task< TResult > > action
        , int retryCount
        , IRetryStrategy? retryStrategy = null
        , CancellationToken cancellationToken = default
        , IEnumerable< Func< Exception, bool > > shouldRetryOnExceptions = null
        , IEnumerable< Func< TResult, bool > > shouldRetryOnResults = null
        , Type[ ] retriableExceptions = null )
    {
        ArgumentNullException.ThrowIfNull( action );
        ArgumentOutOfRangeException.ThrowIfNegative( retryCount );
        retryStrategy ??= new FixedIntervalStrategy( TimeSpan.FromSeconds( 1 ) );
        return await RetryAction(
            action
            , retryStrategy
            , retryCount
            , cancellationToken
            , shouldRetryOnExceptions
            , shouldRetryOnResults
            , retriableExceptions );
    }

    /// <summary>
    /// Executes the specified action with the option to retry on specific exceptions or results.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="action">The action to be executed.</param>
    /// <param name="retryStrategy">The retry strategy to be used.</param>
    /// <param name="retryCount">The number of times to retry the action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="shouldRetryOnExceptions">The list of predicates to determine if an exception should be retried.</param>
    /// <param name="shouldRetryOnResults">The list of predicates to determine if a result should be retried.</param>
    /// <param name="retriableExceptions">The array of exception types to be retried.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task< TResult > RetryAction< TResult >(
        Func< CancellationToken, Task< TResult > > action
        , IRetryStrategy? retryStrategy
        , int retryCount
        , CancellationToken cancellationToken
        , IEnumerable< Func< Exception, bool > > shouldRetryOnExceptions = null
        , IEnumerable< Func< TResult, bool > > shouldRetryOnResults = null
        , Type[ ] retriableExceptions = null )
    {
        cancellationToken.ThrowIfCancellationRequested( );
        List< Exception > exceptions = new( );
        for( int retry = 0; retry <= retryCount; retry++ )
        {
            cancellationToken.ThrowIfCancellationRequested( );
            if( retry > 0 )
            {
                TimeSpan delay = retryStrategy.GetNextDelay( retry );
                if( delay < TimeSpan.Zero )
                    throw new InvalidOperationException( "GetNextDelay must not return a negative delay" );
                await Task.Delay( delay, cancellationToken );
            }

            try
            {
                TResult result = await action( cancellationToken );

                if( shouldRetryOnResults != null && shouldRetryOnResults.Any( predicate => predicate( result ) ) )
                {
                    exceptions.Add( new Exception( "Unexpected result" ) );
                }
                else { return result; }
            }
            catch( Exception ex ) when
                ( retriableExceptions?.Contains( ex.GetType( ) ) ?? true )
            {
                if( shouldRetryOnExceptions != null && !shouldRetryOnExceptions.Any( predicate => predicate( ex ) ) )
                    throw;
                exceptions.Add( ex );
            }
        }

        throw new AggregateException( exceptions );
    }

    /// <summary>
    /// Executes the given action with retry logic based on the specified parameters.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action.</typeparam>
    /// <param name="action">The action to be executed.</param>
    /// <param name="retryCount">The maximum number of times the action should be retried.</param>
    /// <param name="retryStrategy">The strategy for determining the retry interval. If not specified, a default FixedIntervalStrategy with a wait time of 1 second will be used.</param>
    /// <param name="shouldRetryOnExceptions">Optional collection of functions that determine whether an exception should trigger a retry. If not specified, all exceptions will be retried.</param>
    /// <param name="shouldRetryOnResults">Optional collection of functions that determine whether a result should trigger a retry. If not specified, all results will be retried.</param>
    /// <param name="retriableExceptions">Optional collection of exceptions that are retriable. If not specified, all exceptions will be retried.</param>
    /// <returns>The result returned by the action.</returns>
    public static TResult Do< TResult >(
        Func< TResult > action
        , int retryCount
        , IRetryStrategy? retryStrategy = null
        , IEnumerable< Func< Exception, bool > > shouldRetryOnExceptions = null
        , IEnumerable< Func< TResult, bool > > shouldRetryOnResults = null
        , Type[ ] retriableExceptions = null )
    {
        ArgumentNullException.ThrowIfNull( action );
        ArgumentOutOfRangeException.ThrowIfNegative( retryCount );

        retryStrategy ??= new FixedIntervalStrategy( TimeSpan.FromSeconds( 1 ) );

        return RetryAction(
            action
            , retryStrategy
            , retryCount
            , shouldRetryOnExceptions
            , shouldRetryOnResults
            , retriableExceptions );
    }

    /// <summary>
    /// Retries the specified action multiple times based on the provided retry options.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action.</typeparam>
    /// <param name="action">The action to be executed.</param>
    /// <param name="retryStrategy">The retry strategy to be used.</param>
    /// <param name="retryCount">The maximum number of retries.</param>
    /// <param name="shouldRetryOnExceptions">
    /// Optional. The collection of predicates to determine if a specific exception should be retried.
    /// </param>
    /// <param name="shouldRetryOnResults">
    /// Optional. The collection of predicates to determine if a specific result should be retried.
    /// </param>
    /// <param name="retriableExceptions">
    /// Optional. The collection of exception types that should be retried.
    /// </param>
    /// <returns>
    /// The result returned by the action if it succeeds, or throws an <see cref="AggregateException"/>
    /// containing the exceptions encountered during retries.
    /// </returns>
    private static TResult RetryAction< TResult >(
        Func< TResult > action
        , IRetryStrategy? retryStrategy
        , int retryCount
        , IEnumerable< Func< Exception, bool > > shouldRetryOnExceptions = null
        , IEnumerable< Func< TResult, bool > > shouldRetryOnResults = null
        , Type[ ] retriableExceptions = null )
    {
        List< Exception > exceptions = new( );
        for( int retry = 0; retry <= retryCount; retry++ )
        {
            if( retry > 0 )
            {
                TimeSpan delay = retryStrategy.GetNextDelay( retry );
                if( delay < TimeSpan.Zero )
                    throw new InvalidOperationException( "GetNextDelay must not return a negative delay" );
                Task.Delay( delay ).Wait( );
            }

            try
            {
                TResult result = action( );
                if( shouldRetryOnResults != null && shouldRetryOnResults.Any( predicate => predicate( result ) ) )
                {
                    exceptions.Add( new Exception( "Unexpected result" ) );
                }
                else { return result; }
            }
            catch( Exception ex ) when
                ( retriableExceptions?.Contains( ex.GetType( ) ) ?? true )
            {
                if( shouldRetryOnExceptions != null && !shouldRetryOnExceptions.Any( predicate => predicate( ex ) ) )
                    throw;
                exceptions.Add( ex );
            }
        }

        throw new AggregateException( exceptions );
    }
}
