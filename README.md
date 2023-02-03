# Usage Guide for the Retry Object

## Overview
The Retry object provides a convenient way to handle retry logic for your code. There are 3 different methods to handle retry logic for synchronous and asynchronous actions, as well as actions that return a result.

## Retry Strategies
The Retry object provides two retry strategies:

- FixedInterval: waits a constant amount of time before retrying
- ExponentialBackOff: waits an amount of time that increases exponentially before retrying

## Examples

### Retrying a Synchronous Action
```csharp
// An action that throws an exception.
Action action = () => { throw new Exception("Error!"); };

// Retry the action with a fixed interval of 1 second, for a maximum of 3 attempts, using the FixedInterval strategy.
Retry.Do(action, TimeSpan.FromSeconds(1), maxAttemptCount: 3, retryStrategy: RetryStrategy.FixedInterval);
```

### Retrying an Asynchronous Action
```csharp
// An asynchronous action that throws an exception.
Func<Task> asyncAction = async () => { await Task.Delay(1000); throw new Exception("Error!"); };

// Retry the asynchronous action with a fixed interval of 1 second, for a maximum of 3 attempts, using the FixedInterval strategy.
await Retry.DoAsync(asyncAction, TimeSpan.FromSeconds(1), maxAttemptCount: 3, retryStrategy: RetryStrategy.FixedInterval);
```

### Retrying an Action that Returns a Result
```csharp
// An action that returns a result and throws an exception.
Func<int> func = () => { throw new Exception("Error!"); return 1; };

// Retry the action with a fixed interval of 1 second, for a maximum of 3 attempts, using the FixedInterval strategy.
int result = Retry.Do(func, TimeSpan.FromSeconds(1), maxAttemptCount: 3, retryStrategy: RetryStrategy.FixedInterval);
```

###Exception Handling
The Retry object throws an AggregateException if all the retry attempts fail. This exception contains all the individual exceptions that were caught during the retry attempts.

You can catch this exception to handle the failures, for example:
```csharp
try
{
    Retry.Do(action, TimeSpan.FromSeconds(1), maxAttemptCount: 3, retryStrategy: RetryStrategy.FixedInterval);
}
catch (AggregateException ex)
{
    Console.WriteLine("All retry attempts failed.");
    foreach (var innerException in ex.InnerExceptions)
    {
        Console.WriteLine(innerException.Message);
    }
}
```

