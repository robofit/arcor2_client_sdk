using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;

/// <summary>
/// Helper class for testing ObservableCollection.CollectionChanged events 
/// </summary>
public class CollectionChangedAwaiter {
    private readonly TaskCompletionSource<NotifyCollectionChangedEventArgs> tcs = new();
    private readonly CancellationTokenSource cts = new();
    private Func<NotifyCollectionChangedEventArgs, bool>? predicate;

    public CollectionChangedAwaiter() { }

    public CollectionChangedAwaiter(Func<NotifyCollectionChangedEventArgs, bool>? predicate) {
        this.predicate = predicate;
    }

    public void EventHandler(object sender, NotifyCollectionChangedEventArgs e) {
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
    /// Waits for a CollectionChanged event to be raised.
    /// </summary>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <returns>The event arguments that were raised</returns>
    /// <exception cref="TimeoutException">Thrown if the event is not raised within the timeout</exception>
    public async Task<NotifyCollectionChangedEventArgs> WaitForEventAsync(int timeout = 5000) {
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

public static class ObservableCollectionExtensions {
    /// <summary>
    /// Creates an awaiter for the CollectionChanged event
    /// </summary>
    /// <param name="collection">The ObservableCollection to monitor</param>
    /// <param name="action">Optional action type to filter for (Add, Remove, etc.)</param>
    /// <returns>CollectionChangedAwaiter that can be awaited</returns>
    public static CollectionChangedAwaiter CreateCollectionChangedAwaiter<T>(
        this ObservableCollection<T> collection,
        NotifyCollectionChangedAction? action = null) {
        // Create predicate if action filter is specified
        Func<NotifyCollectionChangedEventArgs, bool>? predicate = action.HasValue
            ? e => e.Action == action.Value
            : null;

        var awaiter = new CollectionChangedAwaiter(predicate);
        collection.CollectionChanged += awaiter.EventHandler!;
        return awaiter;
    }

    /// <summary>
    /// Waits for a specific item to be added to the collection
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    /// <param name="collection">The ObservableCollection to monitor</param>
    /// <param name="itemPredicate">Predicate to match the added item</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <returns>The event arguments from when the item was added</returns>
    public static async Task<NotifyCollectionChangedEventArgs> WaitForItemAddedAsync<T>(
        this ObservableCollection<T> collection,
        Func<T, bool> itemPredicate,
        int timeout = 5000) {
        var awaiter = new CollectionChangedAwaiter(e =>
            e.Action == NotifyCollectionChangedAction.Add &&
            e.NewItems!.Count > 0 &&
            e.NewItems.Cast<T>().Any(itemPredicate));

        collection.CollectionChanged += awaiter.EventHandler!;

        try {
            return await awaiter.WaitForEventAsync(timeout);
        }
        finally {
            // Always unsubscribe
            collection.CollectionChanged -= awaiter.EventHandler!;
        }
    }

    /// <summary>
    /// Waits for a specific item to be removed from the collection
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    /// <param name="collection">The ObservableCollection to monitor</param>
    /// <param name="itemPredicate">Predicate to match the removed item</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    public static async Task<NotifyCollectionChangedEventArgs> WaitForItemRemovedAsync<T>(
        this ObservableCollection<T> collection,
        Func<T, bool> itemPredicate,
        int timeout = 5000) {
        var awaiter = new CollectionChangedAwaiter(e =>
            e.Action == NotifyCollectionChangedAction.Remove &&
            e.OldItems!.Count > 0 &&
            e.OldItems.Cast<T>().Any(itemPredicate));

        collection.CollectionChanged += awaiter.EventHandler!;

        try {
            return await awaiter.WaitForEventAsync(timeout);
        }
        finally {
            collection.CollectionChanged -= awaiter.EventHandler!;
        }
    }
}