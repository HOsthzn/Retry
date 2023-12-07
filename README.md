A retry strategy is a technique for handling transient failures, which are temporary and typically resolve themselves without any intervention. These types of failures can occur for various reasons, including network issues, resource contention, or temporary unavailability of an external service.

As a developer, it's important to implement a retry strategy in your code because it can help ensure the reliability and availability of your application. Without a retry strategy, your application may fail when it encounters a transient failure and may not recover without manual intervention. This can result in downtime or data loss, leading to a negative user experience and potentially damaging your reputation.

By implementing a retry strategy, you can mitigate the impact of transient failures by automatically retrying failed operations, thus increasing the chances of success on subsequent attempts. This can help to ensure that your application continues to run smoothly and that your users receive the expected level of service, even in the face of temporary failures.

The specific implementation of a retry strategy will depend on the programming language and technology you're using, as well as the requirements of your specific application. Common approaches include linear backoff, exponential backoff, and fixed retry count, among others.

> The concepts behind the provided Retry objects can easily be adapted to other programming languages and utilized in a similar fashion

In conclusion, implementing a retry strategy is an important step in making your application more reliable and resilient in the face of transient failures. By taking the time to design and implement a retry strategy, you can help ensure the continued availability and success of your application and protect your users from the negative consequences of failures.

___

# Retry Library Documentation

This is a utility library for retrying operations that can encounter transient faults. Retry operations can be
configured to happen synchronously or asynchronously, and return either a void or a type result. The library also
provides interfaces for implementing custom retry strategies.

## Example usages

Let's illustrate the most common usage scenarios.

**1. Retrying asynchronous operations returning `Task<TResult>`:**

```csharp
Func<CancellationToken, Task<int>> action = async (token) => 
{
    // some asynchronous operation that could fail
    // replace with actual implementation
    return await Task.FromResult(1);
};
var result = await Retry.DoAsync(action, 3));
```

In this case, the action is retried 3 times if it fails, using a fixed delay of 1 second between each retry.
> Note: the FixedIntervalStrategy with a delay of 1 second is used by default.

**2. Retrying synchronous operations returning `TResult`:**

```csharp
Func<int> action = () => 
{
    // some synchronous operation that could fail
    // replace with actual implementation
    return 1;
};
var result = Retry.Do(action, 3, new FixedIntervalStrategy(TimeSpan.FromSeconds(3)));
```

In this case, the action is retried 3 times if it fails, using a fixed delay of 3 second between each retry.

**3. For functions that return `void`:**

```csharp
Action action = () => 
{
    // some operation that could fail
    // replace with actual implementation
};

Retry.Do(action, 3, new FixedIntervalStrategy(TimeSpan.FromSeconds(3)));
```

In this case, the action is retried 3 times if it fails, using a fixed delay of 3 second between each retry.

**4. Implementing custom retry strategies:**

You can also implement custom retry strategies by implementing the `IRetryStrategy` interface.

For instance, an exponential back-off strategy could be implemented like below:

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

Then use your custom strategy in the retry operation:

```csharp
Func<int> action = () => 
{
    // some operation that could fail
    // replace with actual implementation
    return 1;
};
var result = Retry.Do(action, 3, new CustomExponentialBackOffStrategy());
```

## Configuring exception handling

You can pass certain conditions that an exception or result must meet in order to trigger a retry. The updated library
now allows this to be specified as a collection of Func delegates and hence can accept multiple conditions:

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

In this case, retry will occur if any of the specified conditions in exception checks or result checks is met.

## ExponentialBackOffWithJitterStrategy example

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

In this example, the action is an asynchronous operation that is retried 3 times if it fails. The delay between each
retry is determined by an ExponentialBackOffWithJitterStrategy which initially waits 1 second, and then increases
exponentially, with a random jitter/delay added.
If a TimeoutException is thrown, the operation would be retried based on the retry strategy provided.

___

## Examples JavaScript
> for this code to work in JavaScript you will need to use ES6

The Retry object provides two functions to perform retries with either a regular or an async function - attempt & attemptAsync. In this section, we will show you how to use these functions to perform retries.

### Performing Retries with the 'Retry.attempt' Function
The Retry.attempt function is used to perform retries with a regular function. It takes four parameters:

- action: The function to be executed.
- retryInterval: The interval between retries, in milliseconds.
- maxAttempts: The maximum number of attempts (defaults to 3).
- strategy: The strategy to use for retrying (defaults to Retry.Strategy.FIXED_INTERVAL).
Here is an example of how to use the Retry.attempt function to perform retries:

```javascript
function myAction() { // Your logic here }
Retry.attempt(myAction, 1000, 5, Retry.Strategy.EXPONENTIAL_BACKOFF);
```

In this example, myAction will be executed up to five times, with a retry interval of 1 second for the first attempt, 2 seconds for the second attempt, 4 seconds for the third attempt, 8 seconds for the fourth attempt, and 16 seconds for the fifth attempt. If all attempts fail, an error will be thrown.

### Performing Retries with the Retry.attemptAsync Function
The Retry.attemptAsync function is used to perform retries with an async function. It takes the same parameters as the Retry.attempt function:

```javascript
async function myAsyncAction() { // Your logic here }
await Retry.attemptAsync(myAsyncAction, 1000, 5, Retry.Strategy.EXPONENTIAL_BACKOFF);
```

In this example, myAsyncAction will be executed up to five times, with a retry interval of 1 second for the first attempt, 2 seconds for the second attempt, 4 seconds for the third attempt, 8 seconds for the fourth attempt, and 16 seconds for the fifth attempt. If all attempts fail, an error will be thrown.

### Setting a Custom Retry Strategy
You can set a custom retry strategy by creating a new function that calculates the retry interval and passing it to the 'Retry.attempt' or 'Retry.attemptAsync' function. For example:

```javascript
function myCustomStrategy(attempted) { return attempted * 1000; }
Retry.attempt(myAction, myCustomStrategy, 5);
```

In this example, the retry interval will be 1 second for the first attempt, 2 seconds for the second attempt, 3 seconds for the third attempt, 4 seconds for the fourth attempt, and 5 seconds for the fifth attempt.
