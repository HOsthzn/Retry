# Usage Guide for the Retry Object In

## Overview
The Retry object provides a convenient way to handle retry logic for your code. There are 3 different methods to handle retry logic for synchronous and asynchronous actions, as well as actions that return a result.

## Retry Strategies
The Retry object provides two retry strategies:

- FixedInterval: waits a constant amount of time before retrying
- ExponentialBackOff: waits an amount of time that increases exponentially before retrying

___

## Examples C#

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

___

## Examples VB.Net

### Retrying a Synchronous Action
```vbnet
' An action that throws an exception.
Dim action As Action = Sub() Throw New Exception("Error!")

' Retry the action with a fixed interval of 1 second, for a maximum of 3 attempts, using the FixedInterval strategy.
Retry.Do(action, TimeSpan.FromSeconds(1), maxAttemptCount:=3, retryStrategy:=RetryStrategy.FixedInterval)
```

### Retrying an Asynchronous Action
```vbnet
' An asynchronous action that throws an exception.
Dim asyncAction As Func(Of Task) = Async Function()
                                        Await Task.Delay(1000)
                                        Throw New Exception("Error!")
                                    End Function

' Retry the asynchronous action with a fixed interval of 1 second, for a maximum of 3 attempts, using the FixedInterval strategy.
Await Retry.DoAsync(asyncAction, TimeSpan.FromSeconds(1), maxAttemptCount:=3, retryStrategy:=RetryStrategy.FixedInterval)
```

### Retrying an Action that Returns a Result
```vbnet
' An action that returns a result and throws an exception.
Dim func As Func(Of Integer) = Function()
                                   Throw New Exception("Error!")
                                   Return 1
                               End Function

' Retry the action with a fixed interval of 1 second, for a maximum of 3 attempts, using the FixedInterval strategy.
Dim result As Integer = Retry.Do(func, TimeSpan.FromSeconds(1), maxAttemptCount:=3, retryStrategy:=RetryStrategy.FixedInterval)
```

###Exception Handling
The Retry object throws an AggregateException if all the retry attempts fail. This exception contains all the individual exceptions that were caught during the retry attempts.

You can catch this exception to handle the failures, for example:
```vbnet
Try
    Retry.Do(action, TimeSpan.FromSeconds(1), maxAttemptCount:=3, retryStrategy:=RetryStrategy.FixedInterval)
Catch ex As AggregateException
    Console.WriteLine("All retry attempts failed.")
    For Each innerException As var In ex.InnerExceptions
        Console.WriteLine(innerException.Message)
    Next
End Try

```


