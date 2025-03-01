using System.Net.WebSockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;

public class Arcor2ServerFixture : IAsyncLifetime {
    private readonly INetwork network;
    private readonly IList<IContainer> containers = new List<IContainer>();
    private readonly string testRunId;
    private readonly string arServerContainerName;

    public string Host => "localhost";
    public ushort Port => 6789;
    public Uri Uri => new($"ws://{Host}:{Port}");

    public Arcor2ServerFixture() {
        testRunId = GenerateRunId();

        network = new NetworkBuilder()
            .WithName($"arcor2-clientsdk-integration-test-network-{testRunId}")
            .Build();
        arServerContainerName = $"fit-demo-arserver-{testRunId}";
    }

    public async Task InitializeAsync() {
        await network.CreateAsync();
        await StartContainersAsync();
        await WaitForServerReadyAsync();
    }

    public async Task DisposeAsync() {
        var stopTasks = containers.Select(container => container.StopAsync()).ToList();
        await Task.WhenAll(stopTasks);
        var disposeTasks = containers.Select(container => container.DisposeAsync().AsTask()).ToList();
        await Task.WhenAll(disposeTasks);

        await network.DeleteAsync();
    }

    private static string GenerateRunId() => Guid.NewGuid().ToString()[..8];

    private async Task StartContainersAsync() {
        var assetContainer = new ContainerBuilder()
            .WithImage("registry.gitlab.com/kinalisoft/test-it-off/asset:2.0.2")
            .WithName($"fit-demo-asset-{testRunId}")
            .WithEnvironment("ASSETS_FOLDER", "/tmp/assets")
            .WithPortBinding("10040", "10040")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-asset")
            .Build();

        containers.Add(assetContainer);
        await assetContainer.StartAsync();

        var projectContainer = new ContainerBuilder()
            .WithImage("registry.gitlab.com/kinalisoft/test-it-off/project:2.0.2")
            .WithName($"fit-demo-project-{testRunId}")
            .WithEnvironment("ASSET_SERVICE_URL", "http://fit-demo-asset:10040")
            .WithPortBinding("10000", "10000")
            .WithPortBinding("10001", "10001")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-project")
            .Build();

        containers.Add(projectContainer);
        await projectContainer.StartAsync();

        var sceneContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_scene:1.1.0")
            .WithName($"fit-demo-scene-{testRunId}")
            .WithPortBinding("5013", "5013")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-scene")
            .Build();

        containers.Add(sceneContainer);
        await sceneContainer.StartAsync();

        var buildContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_build:1.7.0")
            .WithName($"fit-demo-build-{testRunId}")
            .WithEnvironment("ARCOR2_PROJECT_SERVICE_URL", "http://fit-demo-project:10000")
            .WithEnvironment("ARCOR2_PROJECT_PATH", "")
            .WithPortBinding("5008", "5008")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-build")
            .Build();

        containers.Add(buildContainer);
        await buildContainer.StartAsync();

        var executionContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_execution:1.6.0")
            .WithName($"fit-demo-execution-{testRunId}")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_PROJECT_PATH", "/root/project")
            .WithPortBinding("6791", "6790")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-execution")
            .Build();

        containers.Add(executionContainer);
        await executionContainer.StartAsync();

        var calibrationContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_calibration:1.2.0")
            .WithName($"fit-demo-calibration-{testRunId}")
            .WithEnvironment("ARCOR2_CALIBRATION_URL", "http://fit-demo-calibration:5014")
            .WithEnvironment("ARCOR2_CALIBRATION_MOCK", "false")
            .WithPortBinding("5014", "5014")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-calibration")
            .Build();

        containers.Add(calibrationContainer);
        await calibrationContainer.StartAsync();

        var dobotMagicianContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_dobot:1.3.1")
            .WithName($"fit-demo-dobot-magician-{testRunId}")
            .WithEnvironment("ARCOR2_DOBOT_MOCK", "true")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_DOBOT_MODEL", "magician")
            .WithPortBinding("5018", "5018")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-dobot-magician")
            .Build();

        containers.Add(dobotMagicianContainer);
        await dobotMagicianContainer.StartAsync();

        var dobotMagician2Container = new ContainerBuilder()
            .WithImage("arcor2/arcor2_dobot:1.3.1")
            .WithName($"fit-demo-dobot-magician2-{testRunId}")
            .WithEnvironment("ARCOR2_DOBOT_MOCK", "true")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_DOBOT_MODEL", "magician")
            .WithPortBinding("5020", "5018")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-dobot-magician2")
            .Build();

        containers.Add(dobotMagician2Container);
        await dobotMagician2Container.StartAsync();

        var dobotM1Container = new ContainerBuilder()
            .WithImage("arcor2/arcor2_dobot:1.3.1")
            .WithName($"fit-demo-dobot-m1-{testRunId}")
            .WithEnvironment("ARCOR2_DOBOT_MOCK", "true")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_DOBOT_MODEL", "m1")
            .WithPortBinding("5019", "5018")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-dobot-m1")
            .Build();

        containers.Add(dobotM1Container);
        await dobotM1Container.StartAsync();

        var arServerContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_arserver:1.3.1")
            .WithName(arServerContainerName)
            .WithEnvironment("ARCOR2_PROJECT_SERVICE_URL", "http://fit-demo-project:10000")
            .WithEnvironment("ARCOR2_ASSET_SERVICE_URL", "http://fit-demo-asset:10040")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_EXECUTION_URL", "ws://fit-demo-execution:6790")
            .WithEnvironment("ARCOR2_BUILD_URL", "http://fit-demo-build:5008")
            .WithEnvironment("ARCOR2_CALIBRATION_URL", "http://fit-demo-calibration:5014")
            .WithPortBinding(Port, Port)
            .WithPortBinding("6799", "6799")
            .WithNetwork(network)
            .WithNetworkAliases("fit-demo-arserver")
            .Build();

        containers.Add(arServerContainer);
        await arServerContainer.StartAsync();
    }

    private async Task WaitForServerReadyAsync() {
        bool isReady = false;
        int retryCount = 0;
        const int maxRetries = 30;

        while(!isReady && retryCount < maxRetries) {
            try {
                using var client = new ClientWebSocket();
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
                await client.ConnectAsync(Uri, cancellationToken);
                if(client.State == WebSocketState.Open) {
                    isReady = true;
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                }
            }
            catch {
                retryCount++;
                await Task.Delay(1000);
            }
        }

        if(!isReady) {
            throw new TimeoutException("ARCOR2 server did not become ready in time");
        }
    }

    [Fact]
    public async Task ContainerSetup_ConnectToArcor2Server_ShouldSucceed() {
        using var client = new ClientWebSocket();
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        await client.ConnectAsync(Uri, cancellationToken);

        Assert.Equal(WebSocketState.Open, client.State);
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }
}