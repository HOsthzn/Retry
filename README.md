A retry strategy is a technique for handling transient failures, which are temporary and typically resolve themselves without any intervention. These types of failures can occur for various reasons, including network issues, resource contention, or temporary unavailability of an external service.

As a developer, it's important to implement a retry strategy in your code because it can help ensure the reliability and availability of your application. Without a retry strategy, your application may fail when it encounters a transient failure and may not recover without manual intervention. This can result in downtime or data loss, leading to a negative user experience and potentially damaging your reputation.

By implementing a retry strategy, you can mitigate the impact of transient failures by automatically retrying failed operations, thus increasing the chances of success on subsequent attempts. This can help to ensure that your application continues to run smoothly and that your users receive the expected level of service, even in the face of temporary failures.

The specific implementation of a retry strategy will depend on the programming language and technology you're using, as well as the requirements of your specific application. Common approaches include linear backoff, exponential backoff, and fixed retry count, among others.

In conclusion, implementing a retry strategy is an important step in making your application more reliable and resilient in the face of transient failures. By taking the time to design and implement a retry strategy, you can help ensure the continued availability and success of your application and protect your users from the negative consequences of failures.

___
# Retry Library Documentation

> The concepts behind the provided Retry objects can easily be adapted to other programming languages and utilized in a
> similar fashion

This is a utility library for retrying operations that can encounter transient faults. Retry operations can be
configured to happen synchronously or asynchronously, and return either a void or a type result. The code also
provides interfaces for implementing custom retry strategies.

## Example usages

Let's illustrate the most common usage scenarios.

**1. Retrying asynchronous operations returning `Task<TResult>`:**

C#:

```csharp
Func<CancellationToken, Task<int>> action = async (token) => 
{
    // some asynchronous operation that could fail
    // replace with actual implementation
    return await Task.FromResult(1);
};
var result = await Retry.DoAsync(action, 3));
```

VB.NET:

```vb
Dim action As Func(Of CancellationToken, Task(Of Integer))
action = Async Function(token As CancellationToken) As Task(Of Integer)
    ' some asynchronous operation that could fail
    ' replace with actual implementation
    Return Await Task.FromResult(1)
End Function

Dim result = Await Retry.DoAsync(action, 3)
```

In this case, the action is retried 3 times if it fails, using a fixed delay of 1 second between each retry.
> Note: the FixedIntervalStrategy with a delay of 1 second is used by default.

**2. Retrying synchronous operations returning `TResult`:**

C#:

```csharp
Func<int> action = () => 
{
    // some synchronous operation that could fail
    // replace with actual implementation
    return 1;
};
var result = Retry.Do(action, 3, new FixedIntervalStrategy(TimeSpan.FromSeconds(3)));
```

VB.NET:

```vb
Dim action As Func(Of Integer)
action = Function()
    ' some synchronous operation that could fail
    ' replace with actual implementation
    Return 1
End Function

Dim result = Retry.Do(action, 3, New FixedIntervalStrategy(TimeSpan.FromSeconds(3)))
```

In this case, the action is retried 3 times if it fails, using a fixed delay of 3 second between each retry.

**3. For functions that return `void`:**

C#:

```csharp
Action action = () => 
{
    // some operation that could fail
    // replace with actual implementation
};

Retry.Do(action, 3, new FixedIntervalStrategy(TimeSpan.FromSeconds(3)));
```

VB.NET:

```vb
Dim action As Action
action = Sub()
    ' some operation that could fail
    ' replace with actual implementation
End Sub

Retry.Do(action, 3, New FixedIntervalStrategy(TimeSpan.FromSeconds(3)))
```

In this case, the action is retried 3 times if it fails, using a fixed delay of 3 second between each retry.

**4. Implementing custom retry strategies:**

You can also implement custom retry strategies by implementing the `IRetryStrategy` interface.

For instance, an exponential back-off strategy could be implemented like below:

C#:

```csharp
public class CustomExponentialBackOffStrategy: IRetryStrategy
{
    public TimeSpan GetNextDelay(int retryAttempt)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        return delay;
    }
}
```

VB.NET:

```vb
Public Class CustomExponentialBackOffStrategy
    Implements IRetryStrategy

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Dim delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        Return delay
    End Function
End Class
```

Then use your custom strategy in the retry operation:

C#:

```csharp
Func<int> action = () => 
{
    // some operation that could fail
    // replace with actual implementation
    return 1;
};
var result = Retry.Do(action, 3, new CustomExponentialBackOffStrategy());
```

VB.NET:

```vb
Dim action As Func(Of Integer)
action = Function()
    ' some operation that could fail
    ' replace with actual implementation
    Return 1
End Function

Dim result = Retry.Do(action, 3, New CustomExponentialBackOffStrategy())
```

## Configuring exception handling

You can pass certain conditions that an exception or result must meet in order to trigger a retry. The updated library
now allows this to be specified as a collection of Func delegates and hence can accept multiple conditions:

C#:

```csharp
IEnumerable<Func<Exception, bool>> shouldRetryOnExceptions = new List<Func<Exception, bool>> 
{
    ex => ex is TimeoutException,       // retry on timeout exceptions
    ex => ex is NetworkException        // retry on network exceptions
};

IEnumerable<Func<string, bool>> shouldRetryOnResults = new List<Func<string, bool>> 
{
    result => string.IsNullOrEmpty(result),  // retry if string is empty or null 
    result => result.Length < 10,            // retry if string length less than 10
};

Func<int> action = () => 
{
    // some operation that could fail
    // replace with actual implementation
    return 1;
};

var result = Retry.Do(
    action,
    3, 
    new FixedIntervalStrategy(TimeSpan.FromSeconds(1)), 
    shouldRetryOnExceptions, 
    shouldRetryOnResults);
```

VB.NET:

```vb
Dim shouldRetryOnExceptions As IEnumerable(Of Func(Of Exception, Boolean)) = New List(Of Func(Of Exception, Boolean)) From {
    Function(ex) TypeOf ex Is TimeoutException, ' retry on timeout exceptions
    Function(ex) TypeOf ex Is NetworkException ' retry on network exceptions
}

Dim shouldRetryOnResults As IEnumerable(Of Func(Of String, Boolean)) = New List(Of Func(Of String, Boolean)) From {
    Function(result) String.IsNullOrEmpty(result), ' retry if string is empty or null
    Function(result) result.Length < 10 ' retry if string length is less than 10
}

Dim action As Func(Of Integer)
action = Function()
    ' some operation that could fail
    ' replace with actual implementation
    Return 1
End Function

Dim result = Retry.Do(action, 3, New FixedIntervalStrategy(TimeSpan.FromSeconds(1)), shouldRetryOnExceptions, shouldRetryOnResults)
```

In this case, retry will occur if any of the specified conditions in exception checks or result checks is met.

## ExponentialBackOffWithJitterStrategy example

C#:

```csharp
Func<CancellationToken, Task<int>> action = async (token) =>
{
    // some asynchronous operation that could fail
    // replace with actual implementation
    return await Task.FromResult(1);
};

var jitterStrategy = new ExponentialBackOffWithJitterStrategy(TimeSpan.FromSeconds(1));

var result = await Retry.DoAsync(
    action: action,
    retryCount: 3,
    retryStrategy: jitterStrategy,
    cancellationToken: CancellationToken.None,
    shouldRetryOnException: ex => ex is TimeoutException
);
```

VB.NET:

```vb
Dim action As Func(Of CancellationToken, Task(Of Integer))
action = Async Function(token As CancellationToken) As Task(Of Integer)
    ' some asynchronous operation that could fail
    ' replace with actual implementation
    Return Await Task.FromResult(1)
End Function

Dim jitterStrategy = New ExponentialBackOffWithJitterStrategy(TimeSpan.FromSeconds(1))

Dim result = Await Retry.DoAsync(action:=action, retryCount:=3, retryStrategy:=jitterStrategy, cancellationToken:=CancellationToken.None, shouldRetryOnException:=Function(ex) TypeOf ex Is TimeoutException)
```

In this example, the action is an asynchronous operation that is retried 3 times if it fails. The delay between each
retry is determined by an ExponentialBackOffWithJitterStrategy which initially waits 1 second, and then increases
exponentially, with a random jitter/delay added.
If a TimeoutException is thrown, the operation would be retried based on the retry strategy provided.

___

## Examples in JavaScript

For JavaScript ES6:

The Retry object in JavaScript provides two functions, 'attempt' and 'attemptAsync', that perform retries, each with a
regular or an async function. The following demonstrates how to use these functions:

1. ***Retrying with the 'Retry.attempt' Function:*** This function performs retries with regular functions. It has the
   following parameters:
    - `action`: This is the function to be executed.
    - `retryInterval`: This is the interval between retries in milliseconds.
    - `maxAttempts`: This specifies the maximum number of attempts and defaults to 3.
    - `strategy`: This specifies the strategy for retrying operations and defaults to Retry.Strategy.FIXED_INTERVAL.

```javascript
function myAction() {
    // Your logic here 
}

Retry.attempt(myAction, 1000, 5, Retry.Strategy.EXPONENTIAL_BACKOFF);
```

In this example, `myAction` is executed up to five times. If the function fails five times, it throws an error. The
retry interval in this case is 1 second for the first attempt, 2 seconds for the second attempt, 4 seconds for the third
attempt, 8 seconds for the fourth attempt, and 16 seconds for the fifth attempt.

2. ***Retrying with the Retry.attemptAsync Function:*** This function works with async functions and uses the same
   parameters as the `Retry.attempt` function.

```javascript
async function myAsyncAction() {
    // Your logic here 
}

await Retry.attemptAsync(myAsyncAction, 1000, 5, Retry.Strategy.EXPONENTIAL_BACKOFF);
```

In this example, `myAsyncAction` is executed up to five times. If the function fails five times, it throws an error. The
retry interval is 1 second for the first attempt, 2 seconds for the second attempt, 4 seconds for the third attempt, 8
seconds for the fourth attempt, and 16 seconds for the fifth attempt.

3. ***Setting a Custom Retry Strategy:*** For a custom retry strategy, create a new function that calculates the retry
   interval, and pass the function to the `Retry.attempt` or `Retry.attemptAsync` function.

```javascript
function myCustomStrategy(attempted) {
    return attempted * 1000;
}

Retry.attempt(myAction, myCustomStrategy, 5);
```

In this case, the retry interval will be 1 second for the first attempt, 2 seconds for the second attempt, 3 seconds for
the third attempt, 4 seconds for the fourth attempt, and 5 seconds for the fifth attempt.
