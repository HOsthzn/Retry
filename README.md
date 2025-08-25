# Retry Library Documentation

> The concepts behind the provided Retry objects can easily be adapted to other programming languages and utilized in a similar fashion.

This is a utility library for retrying operations that can encounter transient faults. Retry operations can be configured to happen synchronously or asynchronously, and return either a void or a typed result. The code provides interfaces for implementing custom retry strategies.

## Example Usages

Below are the most common usage scenarios. Note that `retryCount` specifies the number of retry attempts after the initial attempt, so total attempts = `retryCount + 1`.

### 1. Retrying Asynchronous Operations Returning `Task<TResult>`

**C#**:

```csharp
Func<CancellationToken, Task<int>> action = async (token) => 
{
    // Some asynchronous operation that could fail
    return await Task.FromResult(1);
};
var result = await Retry.DoAsync(action, retryCount: 3, maxDelay: TimeSpan.FromSeconds(10));
```

**VB.NET**:

```vb
Dim action As Func(Of CancellationToken, Task(Of Integer)) = Async Function(token) As Task(Of Integer)
    ' Some asynchronous operation that could fail
    Return Await Task.FromResult(1)
End Function
Dim result = Await Retry.DoAsync(action, retryCount:=3, maxDelay:=TimeSpan.FromSeconds(10))
```

In this case, the action is retried up to 3 times if it fails, using a fixed delay of 1 second between each retry (default `FixedIntervalStrategy`).

### 2. Retrying Synchronous Operations Returning `TResult`

**C#**:

```csharp
Func<int> action = () => 
{
    // Some synchronous operation that could fail
    return 1;
};
var result = Retry.Do(action, retryCount: 3, retryStrategy: new FixedIntervalStrategy(TimeSpan.FromSeconds(3)), maxDelay: TimeSpan.FromSeconds(10));
```

**VB.NET**:

```vb
Dim action As Func(Of Integer) = Function()
    ' Some synchronous operation that could fail
    Return 1
End Function
Dim result = Retry.Do(action, retryCount:=3, retryStrategy:=New FixedIntervalStrategy(TimeSpan.FromSeconds(3)), maxDelay:=TimeSpan.FromSeconds(10))
```

The action is retried up to 3 times with a fixed delay of 3 seconds between retries.

### 3. For Functions That Return `void`

**C#**:

```csharp
Action action = () => 
{
    // Some operation that could fail
};
Retry.Do(action, retryCount: 3, retryStrategy: new FixedIntervalStrategy(TimeSpan.FromSeconds(3)), maxDelay: TimeSpan.FromSeconds(10));
```

**VB.NET**:

```vb
Dim action As Action = Sub()
    ' Some operation that could fail
End Sub
Retry.Do(action, retryCount:=3, retryStrategy:=New FixedIntervalStrategy(TimeSpan.FromSeconds(3)), maxDelay:=TimeSpan.FromSeconds(10))
```

The action is retried up to 3 times with a fixed delay of 3 seconds between retries.

### 4. Implementing Custom Retry Strategies

You can implement custom retry strategies by implementing the `IRetryStrategy` interface. Here's an example of an exponential back-off strategy:

**C#**:

```csharp
public class CustomExponentialBackOffStrategy : IRetryStrategy
{
    public TimeSpan GetNextDelay(int retryAttempt)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        return delay > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : delay;
    }
}
```

**VB.NET**:

```vb
Public Class CustomExponentialBackOffStrategy
    Implements IRetryStrategy

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Dim delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        Return If(delay > TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), delay)
    End Function
End Class
```

Then use it:

**C#**:

```csharp
Func<int> action = () => 
{
    // Some operation that could fail
    return 1;
};
var result = Retry.Do(action, retryCount: 3, retryStrategy: new CustomExponentialBackOffStrategy(), maxDelay: TimeSpan.FromSeconds(10));
```

**VB.NET**:

```vb
Dim action As Func(Of Integer) = Function()
    ' Some operation that could fail
    Return 1
End Function
Dim result = Retry.Do(action, retryCount:=3, retryStrategy:=New CustomExponentialBackOffStrategy(), maxDelay:=TimeSpan.FromSeconds(10))
```

## Configuring Exception Handling

You can specify conditions for exceptions or results that trigger a retry. If no exception predicates are provided, retries occur for any exception in `retriableExceptions` (or all exceptions if empty). A custom `UnexpectedResultException<T>` is thrown for result-based retries to preserve context.

**C#**:

```csharp
var shouldRetryOnExceptions = new List<Func<Exception, bool>>
{
    ex => ex is TimeoutException,       // Retry on timeout exceptions
    ex => ex is NetworkException        // Retry on network exceptions
};

var shouldRetryOnResults = new List<Func<string, bool>>
{
    result => string.IsNullOrEmpty(result),  // Retry if string is empty or null
    result => result.Length < 10            // Retry if string length is less than 10
};

Func<string> action = () => 
{
    // Some operation that could fail
    return "success";
};

var result = Retry.Do(
    action,
    retryCount: 3,
    retryStrategy: new FixedIntervalStrategy(TimeSpan.FromSeconds(1)),
    shouldRetryOnExceptions: shouldRetryOnExceptions,
    shouldRetryOnResults: shouldRetryOnResults,
    maxDelay: TimeSpan.FromSeconds(10));
```

**VB.NET**:

```vb
Dim shouldRetryOnExceptions As New List(Of Func(Of Exception, Boolean)) From {
    Function(ex) TypeOf ex Is TimeoutException, ' Retry on timeout exceptions
    Function(ex) TypeOf ex Is NetworkException ' Retry on network exceptions
}

Dim shouldRetryOnResults As New List(Of Func(Of String, Boolean)) From {
    Function(result) String.IsNullOrEmpty(result), ' Retry if string is empty or null
    Function(result) result.Length < 10 ' Retry if string length is less than 10
}

Dim action As Func(Of String) = Function()
    ' Some operation that could fail
    Return "success"
End Function

Dim result = Retry.Do(
    action,
    retryCount:=3,
    retryStrategy:=New FixedIntervalStrategy(TimeSpan.FromSeconds(1)),
    shouldRetryOnExceptions:=shouldRetryOnExceptions,
    shouldRetryOnResults:=shouldRetryOnResults,
    maxDelay:=TimeSpan.FromSeconds(10))
```

Retries occur if any exception or result condition is met.

## ExponentialBackOffWithJitterStrategy Example

**C#**:

```csharp
Func<CancellationToken, Task<int>> action = async (token) =>
{
    // Some asynchronous operation that could fail
    return await Task.FromResult(1);
};

var jitterStrategy = new ExponentialBackOffWithJitterStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

var result = await Retry.DoAsync(
    action: action,
    retryCount: 3,
    retryStrategy: jitterStrategy,
    cancellationToken: CancellationToken.None,
    shouldRetryOnException: ex => ex is TimeoutException,
    maxDelay: TimeSpan.FromSeconds(10));
```

**VB.NET**:

```vb
Dim action As Func(Of CancellationToken, Task(Of Integer)) = Async Function(token) As Task(Of Integer)
    ' Some asynchronous operation that could fail
    Return Await Task.FromResult(1)
End Function

Dim jitterStrategy = New ExponentialBackOffWithJitterStrategy(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))

Dim result = Await Retry.DoAsync(
    action:=action,
    retryCount:=3,
    retryStrategy:=jitterStrategy,
    cancellationToken:=CancellationToken.None,
    shouldRetryOnException:=Function(ex) TypeOf ex Is TimeoutException,
    maxDelay:=TimeSpan.FromSeconds(10))
```

The action is retried up to 3 times with exponential back-off (starting at 1 second) plus centered jitter (±20% by default), capped at 10 seconds.

## Examples in JavaScript

The JavaScript `Retry` object provides `attempt` and `attemptAsync` functions for synchronous and asynchronous retries, respectively. Note: Synchronous `attempt` now includes delays but uses a CPU-intensive busy-wait; prefer `attemptAsync` for non-blocking retries.

### 1. Retrying with the `Retry.attempt` Function

For regular or async functions (handled via Promise resolution):

```javascript
function myAction() {
    // Your logic here
    return 42;
}

Retry.attempt(myAction, 1000, 5, Retry.Strategy.EXPONENTIAL_BACKOFF, console.log, 10000);
```

The action is attempted up to 5 times with exponential back-off (1s, 2s, 4s, 8s, 16s, capped at 10s). If `myAction` returns a Promise, it’s awaited synchronously (blocks thread).

### 2. Retrying with the `Retry.attemptAsync` Function

For async functions:

```javascript
async function myAsyncAction() {
    // Your async logic here
    return 42;
}

await Retry.attemptAsync(myAsyncAction, 1000, 5, Retry.Strategy.EXPONENTIAL_BACKOFF, console.log, 10000);
```

The async action is retried up to 5 times with exponential back-off (capped at 10s). If all attempts fail, an `AggregateError` (or fallback `Error`) is thrown with all exceptions.

### 3. Setting a Custom Retry Strategy

Define a custom strategy as a function:

```javascript
function myCustomStrategy(attempted) {
    return Math.min(attempted * 1000, 10000); // Cap at 10s
}

await Retry.attemptAsync(myAsyncAction, myCustomStrategy, 5, console.log, 10000);
```

The retry interval increases linearly (1s, 2s, 3s, 4s, 5s, capped at 10s).
