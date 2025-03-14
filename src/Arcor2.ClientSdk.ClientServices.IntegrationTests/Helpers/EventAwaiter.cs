using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.Managers;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;

/// <summary>
///     Helper class for testing events in xUnit
/// </summary>
/// <typeparam name="TEventArgs">Type of event arguments</typeparam>
public class EventAwaiter<TEventArgs>()
    where TEventArgs : EventArgs {
    private readonly CancellationTokenSource cts = new();
    private readonly Func<TEventArgs, bool>? predicate;
    private readonly TaskCompletionSource<TEventArgs> tcs = new();

    public EventAwaiter(Func<TEventArgs, bool> predicate) : this() {
        this.predicate = predicate;
    }

    public void EventHandler(object? sender, TEventArgs e) {
        try {
            if(predicate == null || predicate(e)) {
                tcs.TrySetResult(e);
            }
        }
        catch(Exception ex) {
            tcs.TrySetException(ex);
        }
    }

    /// <summary>
    ///     Waits for an event to be raised.
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
///     Helper class for testing events in xUnit
/// </summary>
public class EventAwaiter {
    private readonly CancellationTokenSource cts = new();
    private readonly TaskCompletionSource tcs = new();

    public void EventHandler(object? sender, EventArgs e) {
        try {
            tcs.TrySetResult();
        }
        catch(Exception ex) {
            tcs.TrySetException(ex);
        }
    }

    /// <summary>
    ///     Waits for an event to be raised.
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
public static class Arcor2ObjectExtensions {
    public static EventAwaiter GetUpdatedAwaiter<T>(this Arcor2ObjectManager<T> manager) {
        var awaiter = new EventAwaiter();
        manager.Updated += awaiter.EventHandler;
        return awaiter;
    }

    public static Task GetUpdatedAwaiterAndWait<T>(this Arcor2ObjectManager<T> manager) {
        var awaiter = new EventAwaiter();
        manager.Updated += awaiter.EventHandler;
        return awaiter.WaitForEventAsync();
    }

    public static EventAwaiter GetRemovingAwaiter<T>(this Arcor2ObjectManager<T> manager) {
        var awaiter = new EventAwaiter();
        manager.Removing += awaiter.EventHandler;
        return awaiter;
    }

    public static Task GetRemovingAwaiterAndWait<T>(this Arcor2ObjectManager<T> manager) {
        var awaiter = new EventAwaiter();
        manager.Removing += awaiter.EventHandler;
        return awaiter.WaitForEventAsync();
    }

    public static EventAwaiter<SceneOnlineStateEventArgs> GetStartedAwaiter(this SceneManager manager) {
        var awaiter = new EventAwaiter<SceneOnlineStateEventArgs>(p => p.State.State == OnlineState.Started);
        manager.OnlineStateChanged += (o, args) => awaiter.EventHandler(o, args);
        return awaiter;
    }

    public static EventAwaiter<SceneOnlineStateEventArgs> GetStoppedAwaiter(this SceneManager manager) {
        var awaiter = new EventAwaiter<SceneOnlineStateEventArgs>(p => p.State.State == OnlineState.Stopped);
        manager.OnlineStateChanged += (o, args) => awaiter.EventHandler(o, args);
        return awaiter;
    }

    public static EventAwaiter<PackageStateEventArgs> GetPackageStateAwaiter(this PackageManager manager,
        PackageState state) {
        var awaiter = new EventAwaiter<PackageStateEventArgs>(p => p.State == state);
        manager.StateChanged += (o, args) => awaiter.EventHandler(o, args);
        return awaiter;
    }
}