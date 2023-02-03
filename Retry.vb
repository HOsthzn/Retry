Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

Public Class Retry
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
    Public Sub Do(
    action As Action,
    retryInterval As TimeSpan,
    Optional maxAttemptCount As Integer = 3,
    Optional retryStrategy As RetryStrategy = RetryStrategy.FixedInterval)

        ' List to keep track of exceptions that occur during retries.
        Dim exceptions As New List(Of Exception)

        ' Loop through the retry attempts.
        For attempted As Integer = 0 To maxAttemptCount - 1
            Try
                ' Calculate the interval to wait before retrying.
                Dim interval As TimeSpan = If(retryStrategy = RetryStrategy.ExponentialBackOff,
                                         TimeSpan.FromMilliseconds(Math.Pow(2, attempted) * retryInterval.TotalMilliseconds),
                                         retryInterval)

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
    ''' Retry the asynchronous action
    ''' </summary>
    ''' <param name="action">The asynchronous action to retry</param>
    ''' <param name="retryInterval">The interval to wait before retrying</param>
    ''' <param name="maxAttemptCount">The maximum number of attempts to make</param>
    ''' <param name="retryStrategy">The retry strategy (FixedInterval or ExponentialBackOff) (defaults to FixedInterval)</param>
    ''' <returns></returns>
    Public Async Function DoAsync(ByVal action As Func(Of Task), ByVal retryInterval As TimeSpan, Optional maxAttemptCount As Integer = 3, Optional retryStrategy As RetryStrategy = RetryStrategy.FixedInterval) As Task
        ' Create a list to store any exceptions that occur during retries
        Dim exceptions As New List(Of Exception)()

        ' Loop through the retries
        For attempted As Integer = 0 To maxAttemptCount - 1
            Try
                ' Calculate the interval to wait before retrying
                Dim interval As TimeSpan = If(retryStrategy = RetryStrategy.ExponentialBackOff,
                                     TimeSpan.FromMilliseconds(2 ^ attempted * retryInterval.TotalMilliseconds),
                                     retryInterval)

                ' Wait before retrying (except on first attempt)
                If attempted > 0 Then Await Task.Delay(interval)

                ' Attempt the action
                Await action.Invoke()

                ' If the action succeeds, return immediately
                Return
            Catch ex As Exception
                ' Add the exception to the list of exceptions
                exceptions.Add(ex)

                ' Write the exception to the console
                Console.WriteLine($"Failed to complete action, retrying in {retryInterval}: {ex}")
            End Try
        Next

        ' If all retries have failed, throw an AggregateException containing all of the exceptions
        Throw New AggregateException(exceptions)
    End Function
End Class
