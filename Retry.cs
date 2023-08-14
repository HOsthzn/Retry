/// <summary>
    /// Defines a utility class for managing retries of operations.
    /// </summary>
    public static class Retry
    {
        /// <summary>
        /// Interface for defining a strategy for retry intervals.
        /// </summary>
        public interface IRetryStrategy
        {
            /// <summary>
            /// Calculates the interval at which the retry operation should be attempted.
            /// </summary>
            /// <param name="attempted">The number of attempts made so far.</param>
            /// <param name="retryInterval">The time interval to wait before retrying operation.</param>
            /// <returns>The calculated time interval.</returns>
            TimeSpan CalculateInterval( int attempted, TimeSpan retryInterval );
        }

        public class FixedIntervalStrategy : IRetryStrategy
        {
            public TimeSpan CalculateInterval( int attempted, TimeSpan retryInterval ) { return retryInterval; }
        }

        public class ExponentialBackOffStrategy : IRetryStrategy
        {
            public TimeSpan CalculateInterval( int attempted, TimeSpan retryInterval )
            {
                return TimeSpan.FromMilliseconds( Math.Pow( 2, attempted - 1 ) * retryInterval.TotalMilliseconds );
            }
        }

        /// <summary>
        /// Executes the input action after waiting for the calculated interval.
        /// </summary>
        /// <param name="attempted">The number of attempts made so far.</param>
        /// <param name="retryInterval">The time interval between attempts.</param>
        /// <param name="retryStrategy">Strategy to determine the time interval.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public static void ExecuteScheduledTask( int               attempted
                                               , TimeSpan          retryInterval
                                               , IRetryStrategy    retryStrategy
                                               , Action            action
                                               , CancellationToken cancellationToken = default )
        {
            TimeSpan interval = retryStrategy.CalculateInterval( attempted, retryInterval );
            if ( attempted > 0 ) cancellationToken.WaitHandle.WaitOne( interval );
            action.Invoke();
        }

        // Async version of previous method.
        private static async Task ExecuteScheduledTaskAsync( int               attempted
                                                           , TimeSpan          retryInterval
                                                           , IRetryStrategy    retryStrategy
                                                           , Func< Task >      action
                                                           , CancellationToken cancellationToken = default )
        {
            TimeSpan interval = retryStrategy.CalculateInterval( attempted, retryInterval );
            if ( attempted > 0 )
                await Task.Delay( interval, cancellationToken );
            await action();
        }

        // Async version, but also returns a result
        private static async Task< TResult > ExecuteScheduledTaskAsync<TResult>(
            int                     attempted
          , TimeSpan                retryInterval
          , IRetryStrategy?         retryStrategy
          , Func< Task< TResult > > function
          , CancellationToken       cancellationToken = default )
        {
            TimeSpan interval = retryStrategy.CalculateInterval( attempted, retryInterval );
            if ( attempted > 0 )
                await Task.Delay( interval, cancellationToken );
            return await function();
        }

        // Method to handle exceptions. If the exception is not retryable, it is added to the state's list of exceptions and returns false.
        private static bool HandleException( RetryState              state
                                           , Exception               ex
                                           , Predicate< Exception >? isRetryable
                                           , int                     attemptCount
                                           , TimeSpan                retryInterval )
        {
            if ( isRetryable != null && !isRetryable.Invoke( ex ) )
            {
                state.Exceptions.Add( ex );
                return false;
            }

            state.Exceptions.Add( ex );
            state.LogWarning
                ?.Invoke( $"Failed to complete the action on attempt {attemptCount + 1}, retrying in {retryInterval}"
                        , ex );
            return true;
        }

        /// <summary>
        /// Executes an action with retries.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="retryInterval">The time interval between each attempt.</param>
        /// <param name="maxAttemptCount">The maximum number of attempts.</param>
        /// <param name="retryStrategy">The strategy to calculate the interval between attempts.</param>
        /// <param name="isRetryable">The predicate function to determine if the exception is retryable or not.</param>
        /// <param name="logWarning">The action to log a warning message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <param name="exceptionFilter">The function to filter out exceptions.</param>
        public static void Do(
            Action                       action
          , TimeSpan                     retryInterval
          , int                          maxAttemptCount   = 3
          , IRetryStrategy?              retryStrategy     = null
          , Predicate< Exception >?      isRetryable       = null
          , Action< string, Exception >? logWarning        = null
          , CancellationToken            cancellationToken = default
          , Func< Exception, bool >?     exceptionFilter   = null )
        {
            retryStrategy ??= new FixedIntervalStrategy();
            RetryState state = new RetryState { LogWarning = logWarning };

            for ( int attempted = 1; attempted <= maxAttemptCount; attempted++ )
                try
                {
                    ExecuteScheduledTask( attempted, retryInterval, retryStrategy, action, cancellationToken );
                    return;
                }
                catch ( Exception ex ) when ( exceptionFilter?.Invoke( ex ) ?? true )
                {
                    if ( !HandleException( state, ex, isRetryable, attempted, retryInterval ) )
                        throw new AggregateException( state.Exceptions );
                }

            throw new AggregateException( state.Exceptions );
        }

        /// <summary>
        /// Executes a function that returns a result with retries.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        /// <param name="retryInterval">The time interval between each attempt.</param>
        /// <param name="maxAttemptCount">The maximum number of attempts.</param>
        /// <param name="retryStrategy">The strategy to calculate the interval between attempts.</param>
        /// <param name="isRetryable">The predicate function to determine if the exception is retryable or not.</param>
        /// <param name="logWarning">The action to log a warning message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <param name="exceptionFilter">The function to filter out exceptions.</param>
        public static TResult Do<TResult>(
            Func< TResult >              func
          , TimeSpan                     retryInterval
          , int                          maxAttemptCount   = 3
          , IRetryStrategy?              retryStrategy     = null
          , Predicate< Exception >?      isRetryable       = null
          , Action< string, Exception >? logWarning        = null
          , CancellationToken            cancellationToken = default
          , Func< Exception, bool >?     exceptionFilter   = null )
        {
            retryStrategy ??= new FixedIntervalStrategy();
            RetryState state = new RetryState { LogWarning = logWarning };

            for ( int attempted = 1; attempted <= maxAttemptCount; attempted++ )
                try
                {
                    // To capture the result of a function in a delegate, we use a workaround by initializing it outside the delegate
                    TResult result = default!;
                    ExecuteScheduledTask( attempted
                                        , retryInterval
                                        , retryStrategy
                                        , () => { result = func(); }
                                        , cancellationToken );
                    return result;
                }
                catch ( Exception ex )when ( exceptionFilter?.Invoke( ex ) ?? true )
                {
                    if ( !HandleException( state, ex, isRetryable, attempted, retryInterval ) )
                        throw new AggregateException( state.Exceptions );
                }

            throw new AggregateException( state.Exceptions );
        }

        /// <summary>
        /// Executes an async action with retries.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="retryInterval">The time interval between each attempt.</param>
        /// <param name="maxAttemptCount">The maximum number of attempts.</param>
        /// <param name="retryStrategy">The strategy to calculate the interval between attempts.</param>
        /// <param name="isRetryable">The predicate function to determine if the exception is retryable or not.</param>
        /// <param name="logWarning">The action to log a warning message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <param name="exceptionFilter">The function to filter out exceptions.</param>
        public static async Task DoAsync(
            Func< Task >                 func
          , TimeSpan                     retryInterval
          , int                          maxAttemptCount   = 3
          , IRetryStrategy?              retryStrategy     = null
          , Predicate< Exception >?      isRetryable       = null
          , Action< string, Exception >? logWarning        = null
          , CancellationToken            cancellationToken = default
          , Func< Exception, bool >?     exceptionFilter   = null )
        {
            retryStrategy ??= new FixedIntervalStrategy();
            RetryState state = new RetryState { LogWarning = logWarning };

            for ( int attempted = 1; attempted <= maxAttemptCount; attempted++ )
                try
                {
                    await ExecuteScheduledTaskAsync( attempted, retryInterval, retryStrategy, func, cancellationToken );
                    return;
                }
                catch ( Exception ex )when ( exceptionFilter?.Invoke( ex ) ?? true )
                {
                    if ( !HandleException( state, ex, isRetryable, attempted, retryInterval ) )
                        throw new AggregateException( state.Exceptions );
                }

            throw new AggregateException( state.Exceptions );
        }

        /// <summary>
        /// Executes a async function that returns a result with retries.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        /// <param name="retryInterval">The time interval between each attempt.</param>
        /// <param name="maxAttemptCount">The maximum number of attempts.</param>
        /// <param name="retryStrategy">The strategy to calculate the interval between attempts.</param>
        /// <param name="isRetryable">The predicate function to determine if the exception is retryable or not.</param>
        /// <param name="logWarning">The action to log a warning message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <param name="exceptionFilter">The function to filter out exceptions.</param>
        public static async Task< TResult > DoAsync<TResult>(
            Func< Task< TResult > >      func
          , TimeSpan                     retryInterval
          , int                          maxAttemptCount   = 3
          , IRetryStrategy?              retryStrategy     = null
          , Predicate< Exception >?      isRetryable       = null
          , Action< string, Exception >? logWarning        = null
          , CancellationToken            cancellationToken = default
          , Func< Exception, bool >?     exceptionFilter   = null )
        {
            retryStrategy ??= new FixedIntervalStrategy();
            RetryState state = new RetryState { LogWarning = logWarning };

            for ( int attempted = 1; attempted <= maxAttemptCount; attempted++ )
                try
                {
                    TResult result
                        = await ExecuteScheduledTaskAsync( attempted
                                                         , retryInterval
                                                         , retryStrategy
                                                         , func
                                                         , cancellationToken );
                    return result;
                }
                catch ( Exception ex )when ( exceptionFilter?.Invoke( ex ) ?? true )
                {
                    if ( !HandleException( state, ex, isRetryable, attempted, retryInterval ) )
                        throw new AggregateException( state.Exceptions );
                }

            throw new AggregateException( state.Exceptions );
        }

        // Internal class to keep track of retry state.
        private class RetryState
        {
            // Action to log a warning
            public Action< string, Exception >? LogWarning { get; set; }

            // List of exceptions which occured during retries
            public List< Exception > Exceptions { get; }

            public RetryState() { this.Exceptions = new List< Exception >(); }
        }
    }
