# Usage Examples

## C#
```csharp
class Program
{
    static void Main(string[] args)
    {
        Retry.doAsync(() => MakeWebRequest(), 1000, 3, Retry.RetryStrategy.FixedInterval)
            .GetAwaiter()
            .GetResult();
    }

    static async Task MakeWebRequest()
    {
        // code to make a web request
        Console.WriteLine("Making web request");
    }
}
```
## VB.Net#
```vbnet
Public Class Example
    Private Shared Retry As New Retry

    Public Shared Sub Main()
        Dim result As String = Retry.Do(Function() GetDataFromAPI(), 1000, 3, Retry.RetryStrategy.ExponentialBackOff)
        Console.WriteLine(result)
    End Sub

    Private Shared Function GetDataFromAPI() As String
        ' Some code here to retrieve data from an API
        ' ...

        If Not String.IsNullOrEmpty(data) Then
            Return data
        Else
            Throw New Exception("Failed to get data from API")
        End If
    End Function
End Class
```
## JavaScript#
> js will reqire the usage of ES6
``` javascript
function expensiveNetworkCall() {
  console.log("Making network call");
  // some network call implementation that throws error if fails
}

Retry.doAsync(() => {
  expensiveNetworkCall();
}, 1000, 3, Retry.RetryStrategy.ExponentialBackOff)
.catch((err) => {
  console.error("Network call failed after maximum retries: ", err);
});

function readFile(filePath) {
  console.log(`Reading file: ${filePath}`);
  // some file read implementation that throws error if fails
}

Retry.do(() => {
  readFile('sample.txt');
}, 500, 5, Retry.RetryStrategy.FixedInterval)
.catch((err) => {
  console.error("File read failed after maximum retries: ", err);
});
```
