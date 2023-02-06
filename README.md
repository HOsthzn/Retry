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

___

## Examples JavaScript
> for this code to work in JavaScript you will need to be using ES6

The Retry object provides two functions to perform retries with either a regular or an async function. In this section, we will show you how to use these functions to perform retries.

### Performing Retries with the 'Retry.do' Function
The Retry.do function is used to perform retries with a regular function. It takes four parameters:

- action: The function to be executed.
- retryInterval: The interval between retries, in milliseconds.
- maxAttemptCount: The maximum number of attempts (defaults to 3).
- retryStrategy: The strategy to use for retrying (defaults to Retry.RetryStrategy.FixedInterval).
Here is an example of how to use the Retry.do function to perform retries:
```javascript
const Retry = require("./retry");

function myAction() {
  // Your logic here
}

Retry.do(myAction, 1000, 5, Retry.RetryStrategy.ExponentialBackOff);
```
In this example, myAction will be executed up to five times, with a retry interval of 1 second for the first attempt, 2 seconds for the second attempt, 4 seconds for the third attempt, 8 seconds for the fourth attempt, and 16 seconds for the fifth attempt. If all attempts fail, an error will be thrown.

### Performing Retries with the Retry.doAsync Function
The 'Retry.doAsync' function is used to perform retries with an async function. It takes the same parameters as the 'Retry.do' function:
```javascript
const Retry = require("./retry");

async function myAsyncAction() {
  // Your logic here
}

await Retry.doAsync(myAsyncAction, 1000, 5, Retry.RetryStrategy.ExponentialBackOff);
```
In this example, myAsyncAction will be executed up to five times, with a retry interval of 1 second for the first attempt, 2 seconds for the second attempt, 4 seconds for the third attempt, 8 seconds for the fourth attempt, and 16 seconds for the fifth attempt. If all attempts fail, an error will be thrown.

### Setting a Custom Retry Strategy
You can set a custom retry strategy by creating a new function that calculates the retry interval and passing it to the 'Retry.do' or 'Retry.doAsync' function. For example:
```javascript
const Retry = require("./retry");

function myCustomStrategy(attempted) {
  return attempted * 1000;
}

Retry.do(myAction, myCustomStrategy, 5);
```
In this example, the retry interval will be 1 second for the first attempt, 2 seconds for the second attempt, 3 seconds for the third attempt, 4 seconds for the fourth attempt, and 5 seconds for the fifth attempt.

___

## Examples T-SQL

Here are a few examples of how you can use the Retry procedure to retry executing other procedures and functions in SQL.

### Retrying the execution of a stored procedure

You can use the Retry procedure to retry executing another stored procedure in case of failure. Here's an example:
```tsql
DECLARE @tsql NVARCHAR(MAX) = 'EXEC usp_SomeProcedure';
EXEC Retry @tsql, 5, '00:00:10';
```

In this example, the Retry procedure is used to execute the usp_SomeProcedure stored procedure. The @tsql variable contains the T-SQL code to execute, which is passed as an argument to the Retry procedure. The second argument 5 is the number of retries, and the third argument '00:00:10' is the interval time between each retry.

### Retrying the execution of a function

You can also use the Retry procedure to retry executing a function in case of failure. Here's an example:
```tsql
DECLARE @tsql NVARCHAR(MAX) = 'SELECT dbo.fn_SomeFunction()';
EXEC Retry @tsql, 5, '00:00:10';
```

In this example, the Retry procedure is used to execute the fn_SomeFunction function. The @tsql variable contains the T-SQL code to execute, which is passed as an argument to the Retry procedure. The second argument 5 is the number of retries, and the third argument '00:00:10' is the interval time between each retry.

Note that in both examples, if all the retries fail, the original error message will be raised by the Retry procedure.
