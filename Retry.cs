public static class Retry
{
     /// <summary>
    /// Enumeration of the different retry strategies that can be used.
    /// </summary>
    public enum RetryStrategy
    {
        FixedInterval,
        ExponentialBackOff
    }

    /// <summary>
    /// Retry the provided action with specified parameters.
    /// </summary>
    /// <param name="action">The action to retry.</param>
    /// <param name="retryInterval">The time interval to wait before retrying the action.</param>
    /// <param name="maxAttemptCount">The maximum number of attempts to retry the action (default is 3).</param>
    /// <param name="retryStrategy">The retry strategy to use (default is FixedInterval).</param>
    public static void Do(
        Action action,
        TimeSpan retryInterval,
        int maxAttemptCount = 3,
        RetryStrategy retryStrategy = RetryStrategy.FixedInterval)
    {
        // List to keep track of exceptions that occur during retries.
        List<Exception> exceptions = new();

        // Loop through the retry attempts.
        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                // Calculate the interval to wait before retrying.
                TimeSpan interval = retryStrategy == RetryStrategy.ExponentialBackOff
                    ? TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds)
                    : retryInterval;

                // Wait before retrying.
                if (attempted > 0) Task.Delay(interval).Wait();

                // Attempt the action.
                action();

                // If the action succeeds, return.
                return;
            }
            catch (Exception ex)
            {
                // Add the exception to the list of exceptions.
                exceptions.Add(ex);

                // Write the exception to the console.
                Console.WriteLine($"Failed to complete action, retrying in {retryInterval}: {ex.Message}");
            }
        }

        // If all retry attempts fail, throw an AggregateException with all the individual exceptions.
        throw new AggregateException(exceptions);
    }

    /// <summary>
    /// Retry the asynchronous action
    /// </summary>
    /// <param name="action">The asynchronous action to retry</param>
    /// <param name="retryInterval">The interval to wait before retrying</param>
    /// <param name="maxAttemptCount">The maximum number of attempts to make</param>
    /// <param name="retryStrategy">The retry strategy (FixedInterval or ExponentialBackOff) (defaults to FixedInterval)</param>
    /// <returns></returns>
    public static async Task DoAsync(
        Func<Task> action,
        TimeSpan retryInterval,
        int maxAttemptCount = 3,
        RetryStrategy retryStrategy = RetryStrategy.FixedInterval)
    {
        // Create a list to store any exceptions that occur during retries
        List<Exception> exceptions = new List<Exception>();

        // Loop through the retries
        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                // Calculate the interval to wait before retrying
                TimeSpan interval = retryStrategy == RetryStrategy.ExponentialBackOff
                    ? TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds)
                    : retryInterval;

                // Wait before retrying (except on first attempt)
                if (attempted > 0) await Task.Delay(interval);

                // Attempt the action
                await action();

                // If the action succeeds, return immediately
                return;
            }
            catch (Exception ex)
            {
                // Add the exception to the list of exceptions
                exceptions.Add(ex);

                // Write the exception to the console
                Console.WriteLine($"Failed to complete action, retrying in {retryInterval}: {ex}");
            }
        }

        // If all retries have failed, throw an AggregateException containing all of the exceptions
        throw new AggregateException(exceptions);
    }
}
