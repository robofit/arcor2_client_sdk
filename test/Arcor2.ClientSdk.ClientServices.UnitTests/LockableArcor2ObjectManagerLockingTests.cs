using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Managers;
using Arcor2.ClientSdk.ClientServices.UnitTests.Mocks;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.UnitTests;

public class LockableArcor2ObjectManagerLockingTests {
    private Arcor2Session sessionAutoLock;
    private Arcor2Session sessionNoLock;
    private MockLockWebSocket webSocket;

    public LockableArcor2ObjectManagerLockingTests() {
        webSocket = new MockLockWebSocket();
        sessionAutoLock = new Arcor2Session(webSocket, new Arcor2SessionSettings {
            LockingMode = LockingMode.AutoLock, RpcTimeout = 5 // Just instantly timeout
        });
        sessionNoLock = new Arcor2Session(webSocket, new Arcor2SessionSettings {
            LockingMode = LockingMode.NoLocks, RpcTimeout = 5 // Just instantly timeout
        });
        webSocket.ConnectAsync(null!).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Lock_WithAutoLockEnabled_Fails() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => scene.LockAsync());
    }

    [Fact]
    public async Task Lock_WithAutoLockDisabled_Success() {
        var scene = new SceneManager(sessionNoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        try {
            await scene.LockAsync();
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteLockCalled);
    }

    [Fact]
    public async Task Lock_WithAutoLockDisabledAndPaused_Success() {
        var scene = new SceneManager(sessionNoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        try {
            await scene.LockAsync();
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteLockCalled);
    }

    [Fact]
    public async Task Lock_WithAutoLockPaused_Success() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        try {
            await scene.LockAsync();
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteLockCalled);
    }

    [Fact]
    public async Task Unlock_WithAutoLockEnabled_Fails() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => scene.UnlockAsync());
    }

    [Fact]
    public async Task Unlock_WithAutoLockDisabled_Success() {
        var scene = new SceneManager(sessionNoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        try {
            await scene.UnlockAsync();
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteUnlockCalled);
    }

    [Fact]
    public async Task Unlock_WithAutoLockDisabledAndPaused_Success() {
        var scene = new SceneManager(sessionNoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        try {
            await scene.UnlockAsync();
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteUnlockCalled);
    }

    [Fact]
    public async Task Unlock_WithAutoLockPaused_Success() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        try {
            await scene.UnlockAsync();
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteUnlockCalled);
    }

    [Fact]
    public async Task SceneRename_WithAutoLockEnabled_LockInvoked() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        try {
            await scene.RenameAsync("Name");
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteLockCalled);
        Assert.Equal(0u, webSocket.WriteUnlockCalled); // Unlocked by the server.
    }

    [Fact]
    public async Task SceneRename_WithAutoLockDisabled_LockNotInvoked() {
        var scene = new SceneManager(sessionNoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        try {
            await scene.RenameAsync("Name");
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(0u, webSocket.WriteLockCalled);
        Assert.Equal(0u, webSocket.WriteUnlockCalled); // Unlocked by the server.
    }

    [Fact]
    public async Task SceneRename_WithAutoLockPaused_LockNotInvoked() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        try {
            await scene.RenameAsync("Name");
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(0u, webSocket.WriteLockCalled);
        Assert.Equal(0u, webSocket.WriteUnlockCalled); // Unlocked by the server.
    }

    [Fact]
    public async Task SceneRename_WithAutoLockDisabledAndPaused_LockNotInvoked() {
        var scene = new SceneManager(sessionNoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        try {
            await scene.RenameAsync("Name");
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(0u, webSocket.WriteLockCalled);
        Assert.Equal(0u, webSocket.WriteUnlockCalled); // Unlocked by the server.
    }

    [Fact]
    public async Task SceneRename_WithAutoLockedUnpaused_LockInvoked() {
        var scene = new SceneManager(sessionAutoLock,
            new BareScene("name", "desc", DateTime.Now, DateTime.Now, DateTime.Now, "id"));
        scene.PauseAutoLock = true;
        scene.PauseAutoLock = false;
        try {
            await scene.RenameAsync("Name");
        }
        catch(Arcor2ConnectionException) {
            /* Timeout */
        }

        Assert.Equal(1u, webSocket.WriteLockCalled);
        Assert.Equal(0u, webSocket.WriteUnlockCalled); // Unlocked by the server.
    }
}