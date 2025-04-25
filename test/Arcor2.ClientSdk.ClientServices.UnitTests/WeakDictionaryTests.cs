using Arcor2.ClientSdk.ClientServices.Extensions;

// ReSharper disable CollectionNeverUpdated.Local

namespace Arcor2.ClientSdk.ClientServices.UnitTests;

public class WeakDictionaryTests {
    [Fact]
    public void Add_And_Retrieve_Item() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var testObject = new TestObject("Test1");

        // Act
        dictionary.Add("key1", testObject);

        // Assert
        Assert.Single(dictionary);
        Assert.Equal(testObject, dictionary["key1"]);
        Assert.True(dictionary.ContainsKey("key1"));
    }

    [Fact]
    public void Add_And_Remove_Item() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var testObject = new TestObject("Test1");
        dictionary.Add("key1", testObject);

        // Act
        bool removed = dictionary.Remove("key1");

        // Assert
        Assert.True(removed);
        Assert.Empty(dictionary);
        Assert.False(dictionary.ContainsKey("key1"));
        Assert.Null(dictionary["key1"]);
    }

    [Fact]
    public void Remove_NonExistent_Item() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();

        // Act
        var removed = dictionary.Remove("nonexistent");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Clear_Dictionary() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        dictionary.Add("key1", new TestObject("Test1"));
        dictionary.Add("key2", new TestObject("Test2"));

        // Act
        dictionary.Clear();

        // Assert
        Assert.Empty(dictionary);
        Assert.False(dictionary.ContainsKey("key1"));
        Assert.False(dictionary.ContainsKey("key2"));
    }

    [Fact]
    public void Update_Item() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var initialObject = new TestObject("Test1");
        var updatedObject = new TestObject("Test1-Updated");
        dictionary.Add("key1", initialObject);

        // Act
        dictionary["key1"] = updatedObject;

        // Assert
        Assert.Equal(updatedObject, dictionary["key1"]);
    }

    [Fact]
    public void TryGetValue_Existing_Item() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var testObject = new TestObject("Test1");
        dictionary.Add("key1", testObject);

        // Act
        bool result = dictionary.TryGetValue("key1", out var retrievedObject);

        // Assert
        Assert.True(result);
        Assert.Equal(testObject, retrievedObject);
    }

    [Fact]
    public void TryGetValue_NonExistent_Item() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();

        // Act
        bool result = dictionary.TryGetValue("nonexistent", out var retrievedObject);

        // Assert
        Assert.False(result);
        Assert.Null(retrievedObject);
    }

    [Fact]
    public void Keys_Collection() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        dictionary.Add("key1", new TestObject("Test1"));
        dictionary.Add("key2", new TestObject("Test2"));

        // Act
        var keys = dictionary.Keys.ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains(keys, k => k == "key1");
        Assert.Contains(keys, k => k == "key2");
    }

    [Fact]
    public void Values_Collection() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var object1 = new TestObject("Test1");
        var object2 = new TestObject("Test2");
        dictionary.Add("key1", object1);
        dictionary.Add("key2", object2);

        // Act
        var values = dictionary.Values.ToList();

        // Assert
        Assert.Equal(2, values.Count);
        Assert.Contains(values, o => o == object1);
        Assert.Contains(values, o => o == object2);
    }

    [Fact]
    public void Enumeration() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var object1 = new TestObject("Test1");
        var object2 = new TestObject("Test2");
        dictionary.Add("key1", object1);
        dictionary.Add("key2", object2);

        // Act
        int count = 0;
        foreach(var kvp in dictionary) {
            count++;
            Assert.True(kvp.Key is "key1" or "key2");
            if(kvp.Key == "key1") {
                Assert.Equal(object1, kvp.Value);
            }
            else {
                Assert.Equal(object2, kvp.Value);
            }
        }

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void WeakReference_GarbageCollection() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();

        // Act
        AddObjectAndLoseReference(dictionary, "key1");

        for(int i = 0; i < 3; i++) {
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }

        // Assert
        Assert.False(dictionary.ContainsKey("key1"));
        Assert.Null(dictionary["key1"]);
    }

    [Fact]
    public void Multiple_Objects_With_Some_Collected() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();
        var persistentObject = new TestObject("Persistent");

        // Act
        dictionary.Add("persistent", persistentObject);
        AddObjectAndLoseReference(dictionary, "temporary");

        for(int i = 0; i < 3; i++) {
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }

        // Assert
        Assert.True(dictionary.ContainsKey("persistent"));
        Assert.False(dictionary.ContainsKey("temporary"));
        Assert.Equal(persistentObject, dictionary["persistent"]);
        Assert.Null(dictionary["temporary"]);
    }

    [Fact]
    public void Add_Null_Key_Throws() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => dictionary.Add(null!, new TestObject("Test")));
    }

    [Fact]
    public void Add_Null_Value_Throws() {
        // Arrange
        var dictionary = new WeakDictionary<string, TestObject>();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => dictionary.Add("key", null!));
    }

    // Helper method to create an object, add it to dictionary, and remove local reference
    private static void AddObjectAndLoseReference(WeakDictionary<string, TestObject> dictionary, string key) {
        dictionary.Add(key, new TestObject("Temporary"));
    }

    // Test class for use in the dictionary
    private class TestObject(string Name);
}