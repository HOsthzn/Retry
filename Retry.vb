Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Threading
Imports System.Threading.Tasks

Public Interface IRetryStrategy
    Function GetNextDelay(retryAttempt As Integer) As TimeSpan
End Interface

Public Class FixedIntervalStrategy
    Implements IRetryStrategy

    Private ReadOnly _delay As TimeSpan

    Public Sub New(delay As TimeSpan)
        If delay < TimeSpan.Zero Then Throw New ArgumentOutOfRangeException(NameOf(delay), "Delay can't be negative")
        _delay = delay
    End Sub

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Return _delay
    End Function
End Class

Public Class ExponentialBackOffStrategy
    Implements IRetryStrategy

    Private ReadOnly _factor As Double
    Private ReadOnly _initialDelay As TimeSpan
    Private ReadOnly _maxDelay As TimeSpan

    Public Sub New(initialDelay As TimeSpan, maxDelay As TimeSpan, Optional ByVal factor As Double = 2)
        If initialDelay < TimeSpan.Zero Then _
            Throw New ArgumentOutOfRangeException(NameOf(initialDelay), "InitialDelay can't be negative")

        _initialDelay = initialDelay
        _factor = factor
        _maxDelay = maxDelay
    End Sub

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Dim delay As TimeSpan =
                TimeSpan.FromTicks(Convert.ToInt64(_initialDelay.Ticks*Math.Pow(_factor, retryAttempt - 1)))

        If delay > _maxDelay Then
            Return _maxDelay
        Else
            Return delay
        End If
    End Function
End Class

Public Class ExponentialBackOffWithJitterStrategy
    Implements IRetryStrategy

    Private ReadOnly _factor As Double
    Private ReadOnly _initialDelay As TimeSpan
    Private ReadOnly _jitterFactor As Double

    Private ReadOnly _
        _random As RandomNumberGenerator =
            RandomNumberGenerator.Create()

    Public Sub New(initialDelay As TimeSpan, Optional ByVal factor As Double = 2,
                   Optional ByVal jitterFactor As Double = 0.2)
        If initialDelay < TimeSpan.Zero Then _
            Throw New ArgumentOutOfRangeException(NameOf(initialDelay), "InitialDelay can't be negative")

        _initialDelay = initialDelay
        _factor = factor
        _jitterFactor = jitterFactor
    End Sub

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Dim delay As TimeSpan =
                TimeSpan.FromTicks(Convert.ToInt64(_initialDelay.Ticks*Math.Pow(_factor, retryAttempt - 1)))
        Dim jitter As TimeSpan =
                TimeSpan.FromMilliseconds(Math.Abs(delay.TotalMilliseconds*_jitterFactor*(NextDouble()*2 - 1)))

        Return delay + jitter
    End Function

    Private Function NextDouble() As Double
        Dim bytes(7) As Byte ' Specify an array of 8 bytes
        _random.GetBytes(bytes)

        ' Use the BitConverter.ToUInt64 method to convert the byte array to an UInt64
        Dim ul As ULong = BitConverter.ToUInt64(bytes, 0) >> 11

        ' Convert the ul value to a Double for the division operation
        Return ul/(CDbl(ULong.MaxValue >> 11))
    End Function
End Class

Public NotInheritable Class Retry
    Public Shared Async Function DoAsync (Of TResult)(action As Func(Of CancellationToken, Task(Of TResult)),
                                                      retryCount As Integer,
                                                      Optional retryStrategy As IRetryStrategy = Nothing,
                                                      Optional cancellationToken As CancellationToken = Nothing,
                                                      Optional shouldRetryOnExceptions As _
                                                         IEnumerable(Of Func(Of Exception, Boolean)) = Nothing,
                                                      Optional shouldRetryOnResults As _
                                                         IEnumerable(Of Func(Of TResult, Boolean)) = Nothing,
                                                      Optional retriableExceptions As Type() = Nothing) _
        As Task(Of TResult)
        If action Is Nothing Then Throw New ArgumentNullException(NameOf(action))
        If retryCount < 0 Then Throw New ArgumentOutOfRangeException(NameOf(retryCount))

        If retryStrategy Is Nothing Then retryStrategy = New FixedIntervalStrategy(TimeSpan.FromSeconds(1))

        Return _
            Await _
                RetryAction(action, retryStrategy, retryCount, cancellationToken, shouldRetryOnExceptions,
                            shouldRetryOnResults, retriableExceptions)
    End Function

    Private Shared Async Function RetryAction (Of TResult)(action As Func(Of CancellationToken, Task(Of TResult)),
                                                           retryStrategy As IRetryStrategy, retryCount As Integer,
                                                           cancellationToken As CancellationToken,
                                                           Optional shouldRetryOnExceptions As _
                                                              IEnumerable(Of Func(Of Exception, Boolean)) = Nothing,
                                                           Optional shouldRetryOnResults As _
                                                              IEnumerable(Of Func(Of TResult, Boolean)) = Nothing,
                                                           Optional retriableExceptions As Type() = Nothing) _
        As Task(Of TResult)
        If cancellationToken.IsCancellationRequested Then Throw New TaskCanceledException()

        Dim exceptions As New List(Of Exception)

        For retry = 0 To retryCount
            cancellationToken.ThrowIfCancellationRequested()

            If retry > 0 Then
                Dim delay As TimeSpan = retryStrategy.GetNextDelay(retry)
                If delay < TimeSpan.Zero Then _
                    Throw New InvalidOperationException("GetNextDelay must not return a negative delay")
                Await Task.Delay(delay, cancellationToken)
            End If

            Try
                Dim result As TResult = Await action(cancellationToken)
                If _
                    Not _
                    (shouldRetryOnResults Is Nothing OrElse
                     Not shouldRetryOnResults.Any(Function(predicate) predicate(result))) Then
                    exceptions.Add(New Exception("Unexpected result"))
                    Continue For
                End If

                Return result
            Catch ex As Exception When _
                (If(retriableExceptions Is Nothing, True, retriableExceptions.Contains(ex.GetType())))
                If _
                    Not _
                    (shouldRetryOnExceptions Is Nothing OrElse
                     shouldRetryOnExceptions.Any(Function(predicate) predicate(ex))) Then Throw
                exceptions.Add(ex)
            End Try
        Next

        Throw New AggregateException(exceptions)
    End Function

    Public Shared Function [Do] (Of TResult)(action As Func(Of TResult), retryCount As Integer,
                                             Optional retryStrategy As IRetryStrategy = Nothing,
                                             Optional shouldRetryOnExceptions As _
                                                IEnumerable(Of Func(Of Exception, Boolean)) = Nothing,
                                             Optional shouldRetryOnResults As IEnumerable(Of Func(Of TResult, Boolean)) _
                                                = Nothing, Optional retriableExceptions As Type() = Nothing) As TResult
        If action Is Nothing Then Throw New ArgumentNullException(NameOf(action))
        If retryCount < 0 Then Throw New ArgumentOutOfRangeException(NameOf(retryCount))

        If retryStrategy Is Nothing Then retryStrategy = New FixedIntervalStrategy(TimeSpan.FromSeconds(1))

        Return _
            RetryAction(action, retryStrategy, retryCount, shouldRetryOnExceptions, shouldRetryOnResults,
                        retriableExceptions)
    End Function

    Private Shared Function RetryAction (Of TResult)(action As Func(Of TResult), retryStrategy As IRetryStrategy,
                                                     retryCount As Integer,
                                                     Optional shouldRetryOnExceptions As _
                                                        IEnumerable(Of Func(Of Exception, Boolean)) = Nothing,
                                                     Optional shouldRetryOnResults As _
                                                        IEnumerable(Of Func(Of TResult, Boolean)) = Nothing,
                                                     Optional retriableExceptions As Type() = Nothing) As TResult
        Dim exceptions As New List(Of Exception)

        For retry = 0 To retryCount
            If retry > 0 Then
                Dim delay As TimeSpan = retryStrategy.GetNextDelay(retry)
                If delay < TimeSpan.Zero Then _
                    Throw New InvalidOperationException("GetNextDelay must not return a negative delay")
                Task.Delay(delay).Wait()
            End If

            Try
                Dim result As TResult = action()
                If _
                    Not _
                    (shouldRetryOnResults Is Nothing OrElse
                     Not shouldRetryOnResults.Any(Function(predicate) predicate(result))) Then
                    exceptions.Add(New Exception("Unexpected result"))
                    Continue For
                End If

                Return result
            Catch ex As Exception When _
                (If(retriableExceptions Is Nothing, True, retriableExceptions.Contains(ex.GetType())))
                If _
                    Not _
                    (shouldRetryOnExceptions Is Nothing OrElse
                     shouldRetryOnExceptions.Any(Function(predicate) predicate(ex))) Then Throw
                exceptions.Add(ex)
            End Try
        Next

        Throw New AggregateException(exceptions)
    End Function
End Class
