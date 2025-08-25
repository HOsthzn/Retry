Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Threading
Imports System.Threading.Tasks

Option Infer On
Option Strict On

''' <summary>
''' Represents a retry strategy for determining the delay between retry attempts.
''' </summary>
Public Interface IRetryStrategy
    ''' <summary>
    ''' Gets the delay for the specified retry attempt.
    ''' </summary>
    ''' <param name="retryAttempt">The current retry attempt (1-based).</param>
    ''' <returns>The delay duration for the retry attempt.</returns>
    Function GetNextDelay(retryAttempt As Integer) As TimeSpan
End Interface

''' <summary>
''' Represents a retry strategy that uses a fixed delay interval for each retry attempt.
''' </summary>
Public NotInheritable Class FixedIntervalStrategy
    Implements IRetryStrategy

    Private ReadOnly _delay As TimeSpan

    ''' <summary>
    ''' Initializes a new instance of the <see cref="FixedIntervalStrategy"/> class.
    ''' </summary>
    ''' <param name="delay">The fixed time interval between retries.</param>
    ''' <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delay"/> is negative.</exception>
    Public Sub New(delay As TimeSpan)
        If delay < TimeSpan.Zero Then
            Throw New ArgumentOutOfRangeException(NameOf(delay), "Delay cannot be negative.")
        End If
        _delay = delay
    End Sub

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Return _delay
    End Function
End Class

''' <summary>
''' Represents an exponential back-off strategy for retrying operations.
''' </summary>
Public NotInheritable Class ExponentialBackOffStrategy
    Implements IRetryStrategy

    Private ReadOnly _factor As Double
    Private ReadOnly _initialDelay As TimeSpan
    Private ReadOnly _maxDelay As TimeSpan

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ExponentialBackOffStrategy"/> class.
    ''' </summary>
    ''' <param name="initialDelay">The initial delay before the first retry.</param>
    ''' <param name="maxDelay">The maximum delay between retries.</param>
    ''' <param name="factor">The factor by which the delay increases after each retry (default is 2).</param>
    ''' <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelay"/> is negative.</exception>
    Public Sub New(initialDelay As TimeSpan, maxDelay As TimeSpan, Optional factor As Double = 2)
        If initialDelay < TimeSpan.Zero Then
            Throw New ArgumentOutOfRangeException(NameOf(initialDelay), "Initial delay cannot be negative.")
        End If
        _initialDelay = initialDelay
        _factor = factor
        _maxDelay = maxDelay
    End Sub

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Try
            Checked
                Dim delayTicks = CLng(_initialDelay.Ticks * Math.Pow(_factor, retryAttempt - 1)) ' Added overflow check
            End Checked
        Catch ex As OverflowException
            Return _maxDelay ' Cap on overflow
        End Try
        Dim delay = TimeSpan.FromTicks(delayTicks)
        Return If(delay > _maxDelay, _maxDelay, delay)
    End Function
End Class

''' <summary>
''' Represents a retry strategy that implements exponential back-off with jitter.
''' </summary>
Public NotInheritable Class ExponentialBackOffWithJitterStrategy
    Implements IRetryStrategy

    Private ReadOnly _factor As Double
    Private ReadOnly _initialDelay As TimeSpan
    Private ReadOnly _jitterFactor As Double
    Private ReadOnly _maxDelay As TimeSpan ' Added maxDelay for consistency and to cap growth
    Private ReadOnly _random As RandomNumberGenerator = RandomNumberGenerator.Create()

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ExponentialBackOffWithJitterStrategy"/> class.
    ''' </summary>
    ''' <param name="initialDelay">The initial delay before the first retry.</param>
    ''' <param name="maxDelay">The maximum delay between retries.</param>
    ''' <param name="factor">The multiplier for delay increase per retry (default is 2).</param>
    ''' <param name="jitterFactor">The factor for applying jitter (default is 0.2).</param>
    ''' <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelay"/> is negative.</exception>
    Public Sub New(initialDelay As TimeSpan, maxDelay As TimeSpan, Optional factor As Double = 2, Optional jitterFactor As Double = 0.2)
        If initialDelay < TimeSpan.Zero Then
            Throw New ArgumentOutOfRangeException(NameOf(initialDelay), "Initial delay cannot be negative.")
        End If
        _initialDelay = initialDelay
        _factor = factor
        _jitterFactor = jitterFactor
        _maxDelay = maxDelay
    End Sub

    Public Function GetNextDelay(retryAttempt As Integer) As TimeSpan Implements IRetryStrategy.GetNextDelay
        Dim delayTicks = CLng(_initialDelay.Ticks * Math.Pow(_factor, retryAttempt - 1))
        Dim baseDelay = TimeSpan.FromTicks(delayTicks)
        If baseDelay > _maxDelay Then baseDelay = _maxDelay ' Cap base delay

        Dim jitterMs = baseDelay.TotalMilliseconds * _jitterFactor * (NextDouble() * 2 - 1) ' Signed jitter (centered)
        Dim finalDelayMs = baseDelay.TotalMilliseconds + jitterMs
        Return TimeSpan.FromMilliseconds(Math.Max(0, finalDelayMs)) ' Prevent negative delays
    End Function

    Private Function NextDouble() As Double
        Dim bytes(7) As Byte
        _random.GetBytes(bytes)
        Dim ul = BitConverter.ToUInt64(bytes, 0) >> 11
        Return ul / CDbl(1UL << 53) ' Aligned with C# fix for exact [0, 1)
    End Function
End Class

''' <summary>
''' Custom exception for unexpected results during retries.
''' </summary>
Public Class UnexpectedResultException(Of TResult)
    Inherits Exception

    Public ReadOnly Property Result As TResult

    Public Sub New(result As TResult)
        MyBase.New("Unexpected result")
        Me.Result = result
    End Sub
End Class

''' <summary>
''' Provides methods for retrying actions with configurable retry strategies and conditions.
''' </summary>
Public NotInheritable Class Retry
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Executes an asynchronous action with retry logic. Total attempts = retryCount + 1.
    ''' </summary>
    ''' <typeparam name="TResult">The type of the result.</typeparam>
    ''' <param name="action">The asynchronous action to execute.</param>
    ''' <param name="retryCount">The maximum number of retry attempts.</param>
    ''' <param name="retryStrategy">The strategy for retry delays. Defaults to <see cref="FixedIntervalStrategy"/> with 1-second delay.</param>
    ''' <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    ''' <param name="shouldRetryOnExceptions">Predicates to determine if an exception should trigger a retry.</param>
    ''' <param name="shouldRetryOnResults">Predicates to determine if a result should trigger a retry.</param>
    ''' <param name="retriableExceptions">Exception types that are retriable.</param>
    ''' <returns>A task representing the result of the action.</returns>
    ''' <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    ''' <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retryCount"/> is negative.</exception>
    ''' <exception cref="AggregateException">Thrown when all retries fail.</exception>
    Public Shared Async Function DoAsync(Of TResult)(
        action As Func(Of CancellationToken, Task(Of TResult)),
        retryCount As Integer,
        Optional retryStrategy As IRetryStrategy = Nothing,
        Optional cancellationToken As CancellationToken = Nothing,
        Optional shouldRetryOnExceptions As IEnumerable(Of Func(Of Exception, Boolean)) = Nothing,
        Optional shouldRetryOnResults As IEnumerable(Of Func(Of TResult, Boolean)) = Nothing,
        Optional retriableExceptions As ReadOnlySpan(Of Type) = Nothing) As Task(Of TResult)

        ArgumentNullException.ThrowIfNull(action)
        If retryCount < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(retryCount), "Retry count cannot be negative.")
        End If

        retryStrategy = If(retryStrategy, New FixedIntervalStrategy(TimeSpan.FromSeconds(1)))
        Return Await RetryActionAsync(action, retryStrategy, retryCount, cancellationToken, shouldRetryOnExceptions, shouldRetryOnResults, retriableExceptions)
    End Function

    Private Shared Async Function RetryActionAsync(Of TResult)(
        action As Func(Of CancellationToken, Task(Of TResult)),
        retryStrategy As IRetryStrategy,
        retryCount As Integer,
        cancellationToken As CancellationToken,
        shouldRetryOnExceptions As IEnumerable(Of Func(Of Exception, Boolean)),
        shouldRetryOnResults As IEnumerable(Of Func(Of TResult, Boolean)),
        retriableExceptions As ReadOnlySpan(Of Type)) As Task(Of TResult)

        cancellationToken.ThrowIfCancellationRequested()
        Dim exceptions As New List(Of Exception)(retryCount + 1) ' Preallocate capacity

        For retry = 0 To retryCount
            cancellationToken.ThrowIfCancellationRequested()

            If retry > 0 Then
                Dim delay = retryStrategy.GetNextDelay(retry)
                If delay < TimeSpan.Zero Then
                    Throw New InvalidOperationException("Retry delay cannot be negative.")
                End If
                Await Task.Delay(delay, cancellationToken)
            End If

            Try
                Dim result = Await action(cancellationToken)
                If shouldRetryOnResults?.Any(Function(predicate) predicate(result)) = True Then
                    exceptions.Add(New UnexpectedResultException(Of TResult)(result)) ' Preserve result context
                    Continue For
                End If
                Return result
            Catch ex As Exception When retriableExceptions.IsEmpty OrElse retriableExceptions.Contains(ex.GetType())
                Dim shouldRetry = If(shouldRetryOnExceptions?.Any(Function(predicate) predicate(ex)), True) ' Fixed: Default to retry if no predicates
                If Not shouldRetry Then
                    Throw
                End If
                exceptions.Add(ex)
            End Try
        Next

        Throw New AggregateException(exceptions)
    End Function

    ''' <summary>
    ''' Executes a synchronous action with retry logic. Total attempts = retryCount + 1.
    ''' </summary>
    ''' <typeparam name="TResult">The type of the result.</typeparam>
    ''' <param name="action">The action to execute.</param>
    ''' <param name="retryCount">The maximum number of retry attempts.</param>
    ''' <param name="retryStrategy">The strategy for retry delays. Defaults to <see cref="FixedIntervalStrategy"/> with 1-second delay.</param>
    ''' <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    ''' <param name="shouldRetryOnExceptions">Predicates to determine if an exception should trigger a retry.</param>
    ''' <param name="shouldRetryOnResults">Predicates to determine if a result should trigger a retry.</param>
    ''' <param name="retriableExceptions">Exception types that are retriable.</param>
    ''' <returns>The result of the action.</returns>
    ''' <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    ''' <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retryCount"/> is negative.</exception>
    ''' <exception cref="AggregateException">Thrown when all retries fail.</exception>
    Public Shared Function Do(Of TResult)(
        action As Func(Of TResult),
        retryCount As Integer,
        Optional retryStrategy As IRetryStrategy = Nothing,
        Optional cancellationToken As CancellationToken = Nothing,
        Optional shouldRetryOnExceptions As IEnumerable(Of Func(Of Exception, Boolean)) = Nothing,
        Optional shouldRetryOnResults As IEnumerable(Of Func(Of TResult, Boolean)) = Nothing,
        Optional retriableExceptions As ReadOnlySpan(Of Type) = Nothing) As TResult

        ArgumentNullException.ThrowIfNull(action)
        If retryCount < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(retryCount), "Retry count cannot be negative.")
        End If

        retryStrategy = If(retryStrategy, New FixedIntervalStrategy(TimeSpan.FromSeconds(1)))
        Return RetryAction(action, retryStrategy, retryCount, cancellationToken, shouldRetryOnExceptions, shouldRetryOnResults, retriableExceptions)
    End Function

    Private Shared Function RetryAction(Of TResult)(
        action As Func(Of TResult),
        retryStrategy As IRetryStrategy,
        retryCount As Integer,
        cancellationToken As CancellationToken,
        shouldRetryOnExceptions As IEnumerable(Of Func(Of Exception, Boolean)),
        shouldRetryOnResults As IEnumerable(Of Func(Of TResult, Boolean)),
        retriableExceptions As ReadOnlySpan(Of Type)) As TResult

        cancellationToken.ThrowIfCancellationRequested()
        Dim exceptions As New List(Of Exception)(retryCount + 1) ' Preallocate capacity

        For retry = 0 To retryCount
            cancellationToken.ThrowIfCancellationRequested()

            If retry > 0 Then
                Dim delay = retryStrategy.GetNextDelay(retry)
                If delay < TimeSpan.Zero Then
                    Throw New InvalidOperationException("Retry delay cannot be negative.")
                End If
                Task.Delay(delay, cancellationToken).Wait(cancellationToken)
            End If

            Try
                Dim result = action()
                If shouldRetryOnResults?.Any(Function(predicate) predicate(result)) = True Then
                    exceptions.Add(New UnexpectedResultException(Of TResult)(result)) ' Preserve result context
                    Continue For
                End If
                Return result
            Catch ex As Exception When retriableExceptions.IsEmpty OrElse retriableExceptions.Contains(ex.GetType())
                Dim shouldRetry = If(shouldRetryOnExceptions?.Any(Function(predicate) predicate(ex)), True) ' Fixed: Default to retry if no predicates
                If Not shouldRetry Then
                    Throw
                End If
                exceptions.Add(ex)
            End Try
        Next

        Throw New AggregateException(exceptions)
    End Function
End Class
