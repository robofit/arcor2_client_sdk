using System;
using System.Collections;
using System.Collections.Generic;

// Inspired by the following article and stackoverflow answer:
// https://dev.to/bhaeussermann/creating-a-weak-dictionary-in-net-1fo
// https://stackoverflow.com/a/2784374

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    /// <summary>
    /// Dictionary using weak references. Not thread-safe.
    /// </summary>
    public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TValue : class {
        private readonly Dictionary<TKey, WeakReference<TValue>> dictionary = new Dictionary<TKey, WeakReference<TValue>>();

        public TValue this[TKey key] {
            get {
                if(dictionary.TryGetValue(key, out var wf) &&
                   wf.TryGetTarget(out var value)) {
                    return value;
                }
                return null!;
            }
            set {
                if(key == null) {
                    throw new ArgumentNullException(nameof(key));
                }

                if(value == null) {
                    throw new ArgumentNullException(nameof(value));
                }

                if(dictionary.TryGetValue(key, out var weakRef)) {
                    weakRef.SetTarget(value);
                }
                else {
                    dictionary[key] = new WeakReference<TValue>(value);
                }
            }
        }

        public ICollection<TKey> Keys => CleanupAndGetKeys();

        public ICollection<TValue> Values => CleanupAndGetValues();

        public int Count {
            get {
                Cleanup();
                return dictionary.Count;
            }
        }

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if(value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            if(dictionary.ContainsKey(key)) {
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            }

            dictionary.Add(key, new WeakReference<TValue>(value));
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        public void Clear() {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            if(item.Key == null || item.Value == null) {
                return false;
            }

            if(!dictionary.TryGetValue(item.Key, out var weakRef)) {
                return false;
            }

            if(!weakRef.TryGetTarget(out var value)) {
                return false;
            }

            // This adds proper equality support for value types and custom comparers.
            return EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        public bool ContainsKey(TKey key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if(!dictionary.TryGetValue(key, out var weakRef)) {
                return false;
            }

            if(!weakRef.TryGetTarget(out _)) {
                dictionary.Remove(key);
                return false;
            }

            return true;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            if(array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            if(arrayIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if(array.Length - arrayIndex < Count) {
                throw new ArgumentException("The number of elements in the source is greater than the available space in the array.");
            }

            foreach(var kvp in this) {
                array[arrayIndex++] = kvp;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            Cleanup();

            foreach(var kvp in dictionary) {
                if(kvp.Value.TryGetTarget(out var target)) {
                    yield return new KeyValuePair<TKey, TValue>(kvp.Key, target);
                }
            }
        }

        public bool Remove(TKey key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            if(item.Key == null || item.Value == null) {
                return false;
            }

            if(!dictionary.TryGetValue(item.Key, out var weakRef)) {
                return false;
            }

            if(!weakRef.TryGetTarget(out var value)) {
                return false;
            }

            if(EqualityComparer<TValue>.Default.Equals(value, item.Value)) {
                return dictionary.Remove(item.Key);
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value) {
            value = default!;

            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if(!dictionary.TryGetValue(key, out var weakRef)) {
                return false;
            }

            if(!weakRef.TryGetTarget(out value)) {
                dictionary.Remove(key);
                return false;
            }

            return true;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        // Helper methods to clean up and retrieve collections
        private void Cleanup() {
            var keysToRemove = new List<TKey>();

            foreach(var kvp in dictionary) {
                if(!kvp.Value.TryGetTarget(out _)) {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach(var key in keysToRemove) {
                dictionary.Remove(key);
            }
        }

        private List<TKey> CleanupAndGetKeys() {
            var result = new List<TKey>();
            var keysToRemove = new List<TKey>();

            foreach(var kvp in dictionary) {
                if(kvp.Value.TryGetTarget(out _)) {
                    result.Add(kvp.Key);
                }
                else {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach(var key in keysToRemove) {
                dictionary.Remove(key);
            }

            return result;
        }

        private List<TValue> CleanupAndGetValues() {
            var result = new List<TValue>();
            var keysToRemove = new List<TKey>();

            foreach(var kvp in dictionary) {
                if(kvp.Value.TryGetTarget(out var target)) {
                    result.Add(target);
                }
                else {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach(var key in keysToRemove) {
                dictionary.Remove(key);
            }

            return result;
        }
    }
}