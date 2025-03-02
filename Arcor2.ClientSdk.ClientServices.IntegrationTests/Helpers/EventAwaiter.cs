namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;

/// <summary>
/// Helper class for testing events in xUnit
/// </summary>
/// <typeparam name="TEventArgs">Type of event arguments</typeparam>
public class EventAwaiter<TEventArgs>
    where TEventArgs : EventArgs {
    private readonly TaskCompletionSource<TEventArgs> tcs = new();
    private readonly CancellationTokenSource cts = new();

    public void EventHandler(object? sender, TEventArgs e) {
        try {
            tcs.TrySetResult(e);
        }
        catch(Exception ex) {
            tcs.TrySetException(ex);
        }
    }

    /// <summary>
    /// Waits for an event to be raised.
    /// </summary>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <returns>The event arguments that were raised</returns>
    /// <exception cref="TimeoutException">Thrown if the event is not raised within the timeout</exception>
    public async Task<TEventArgs> WaitForEventAsync(int timeout = 5000) {
        using var cts = new CancellationTokenSource(timeout);

        try {
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cts.Token));

            if(completedTask == tcs.Task) {
                return await tcs.Task;
            }
            else {
                throw new TimeoutException($"Event was not raised within {timeout}ms");
            }
        }
        finally {
            cts.Cancel();
        }
    }
}

/// <summary>
/// Helper class for testing events in xUnit
/// </summary>
public class EventAwaiter {
    private readonly TaskCompletionSource tcs = new();
    private readonly CancellationTokenSource cts = new();

    public void EventHandler(object? sender, EventArgs e) {
        try {
            tcs.TrySetResult();
        }
        catch(Exception ex) {
            tcs.TrySetException(ex);
        }
    }

    /// <summary>
    /// Waits for an event to be raised.
    /// </summary>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <returns>The event arguments that were raised</returns>
    /// <exception cref="TimeoutException">Thrown if the event is not raised within the timeout</exception>
    public async Task WaitForEventAsync(int timeout = 5000) {
        cts.CancelAfter(timeout);

        var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(
            t => throw new TimeoutException($"Event was not raised within {timeout}ms"),
            TaskContinuationOptions.ExecuteSynchronously);

        await Task.WhenAny(tcs.Task, timeoutTask);

        await cts.CancelAsync();
        await tcs.Task;
    }
}