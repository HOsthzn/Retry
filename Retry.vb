Public Module Retry
    ''' <summary>
    ''' Enumeration of the different retry strategies that can be used.
    ''' </summary>
    Public Enum RetryStrategy
        FixedInterval
        ExponentialBackOff
    End Enum

    ''' <summary>
    ''' Retry the provided action with specified parameters.
    ''' </summary>
    ''' <param name="action">The action to retry.</param>
    ''' <param name="retryInterval">The time interval to wait before retrying the action.</param>
    ''' <param name="maxAttemptCount">The maximum number of attempts to retry the action (default is 3).</param>
    ''' <param name="retryStrategy">The retry strategy to use (default is FixedInterval).</param>
    Public Shared Sub Do(ByVal action As Action, ByVal retryInterval As TimeSpan, Optional ByVal maxAttemptCount As Integer = 3, Optional ByVal retryStrategy As RetryStrategy = RetryStrategy.FixedInterval)
        ' List to keep track of exceptions that occur during retries.
        Dim exceptions As New List(Of Exception)

        ' Loop through the retry attempts.
        For attempted As Integer = 0 To maxAttemptCount - 1
            Try
                ' Calculate the interval to wait before retrying.
                Dim interval As TimeSpan = If(retryStrategy = RetryStrategy.ExponentialBackOff, TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds), retryInterval)

                ' Wait before retrying.
                If attempted > 0 Then Task.Delay(interval).Wait()

                ' Attempt the action.
                action()

                ' If the action succeeds, return.
                Return
            Catch ex As Exception
                ' Add the exception to the list of exceptions.
                exceptions.Add(ex)

                ' Write the exception to the console.
                Console.WriteLine($"Failed to complete action, retrying in {retryInterval}: {ex.Message}")
            End Try
        Next

        ' If all retry attempts fail, throw an AggregateException with all the individual exceptions.
        Throw New AggregateException(exceptions)
    End Sub

    ''' <summary>
    ''' Retry the provided action with specified parameters.
    ''' </summary>
    ''' <typeparam name="TResult">The type of the result expected from the action to be retried.</typeparam>
    ''' <param name="func">The action to be retried.</param>
    ''' <param name="retryInterval">The time interval to wait before retrying the action.</param>
    ''' <param name="maxAttemptCount">The maximum number of attempts to retry the action (default is 3).</param>
    ''' <param name="retryStrategy">The retry strategy to use (default is FixedInterval).</param>
    ''' <returns>
    ''' The result of the action if it succeeds, otherwise an AggregateException with all the caught exceptions is
    ''' thrown.
    ''' </returns>
    Public Shared Function Do(Of TResult)(func As Func(Of TResult), retryInterval As TimeSpan, Optional maxAttemptCount As Integer = 3, Optional retryStrategy As RetryStrategy = RetryStrategy.FixedInterval) As TResult
        Dim exceptions As New List(Of Exception)()
        For attempted As Integer = 0 To maxAttemptCount - 1
            Try
                Dim interval As TimeSpan = If(retryStrategy = RetryStrategy.ExponentialBackOff, TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds), retryInterval)
                If attempted > 0 Then Task.Delay(interval).Wait()
                Return func()
            Catch ex As Exception
                exceptions.Add(ex)
                Console.WriteLine("Failed to complete action, retrying in {0}: {1}", retryInterval, ex.Message)
            End Try
        Next
        Throw New AggregateException(exceptions)
    End Function

    ''' <summary>
    ''' Retry the asynchronous action
    ''' </summary>
    ''' <param name="action">The asynchronous action to retry</param>
    ''' <param name="retryInterval">The interval to wait before retrying</param>
    ''' <param name="maxAttemptCount">The maximum number of attempts to make</param>
    ''' <param name="retryStrategy">The retry strategy to use</param>
    ''' <returns></returns>
    Public Shared Async Function DoAsync(action As Func(Of Task), retryInterval As TimeSpan, Optional maxAttemptCount As Integer = 3, Optional retryStrategy As RetryStrategy = RetryStrategy.FixedInterval) As Task
        Dim exceptions As New List(Of Exception)()
        For attempted As Integer = 0 To maxAttemptCount - 1
            Try
                Dim interval As TimeSpan = If(retryStrategy = RetryStrategy.ExponentialBackOff, TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds), retryInterval)
                If attempted > 0 Then Await Task.Delay(interval)
                Await action()
                Return
            Catch ex As Exception
                exceptions.Add(ex)
                Console.WriteLine("Failed to complete action, retrying in {0}: {1}", retryInterval, ex.Message)
            End Try
        Next
        Throw New AggregateException(exceptions)
    End Function

    ''' <summary>
    ''' Retry the asynchronous action
    ''' </summary>
    ''' <typeparam name="TResult">The result type of the asynchronous action</typeparam>
    ''' <param name="func">The asynchronous action to retry</param>
    ''' <param name="retryInterval">The interval to wait before retrying</param>
    ''' <param name="maxAttemptCount">The maximum number of attempts to make</param>
    ''' <param name="retryStrategy">The retry strategy to use</param>
    ''' <returns>The result of the function if it executes successfully, or throws an `AggregateException` if all retries fail.</returns>
    Public Shared Async Function DoAsync(Of TResult)(ByVal func As Func(Of Task(Of TResult)),
    ByVal retryInterval As TimeSpan,
    Optional ByVal maxAttemptCount As Integer = 3,
    Optional ByVal retryStrategy As RetryStrategy = RetryStrategy.FixedInterval) As Task(Of TResult)

        ' A list to store all exceptions thrown by the function
        Dim exceptions As New List(Of Exception)

        ' Loop for the maximum number of attempts
        For attempted As Integer = 0 To maxAttemptCount - 1
            Try
                ' Determine the interval to wait before retrying based on the retry strategy
                Dim interval As TimeSpan = If(retryStrategy = RetryStrategy.ExponentialBackOff,
                                             TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds),
                                             retryInterval)

                ' Wait for the interval if this is not the first attempt
                If attempted > 0 Then Await Task.Delay(interval)

                ' Try to execute the function
                Return Await func()
            Catch ex As Exception
                ' Add the exception to the list and log a message indicating that the function failed and will be retried
                exceptions.Add(ex)
                Console.WriteLine("Failed to complete action, retrying in " & retryInterval & ": " & ex.ToString())
            End Try
        Next

        ' If all retries fail, throw an `AggregateException` containing all exceptions thrown by the function
        Throw New AggregateException(exceptions)
    End Function

End Module
