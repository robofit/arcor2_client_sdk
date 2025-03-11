using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;

public class Arcor2ServerFixture : IAsyncLifetime {
    private readonly List<IContainer> containers = new();
    private readonly Dictionary<string, INetwork> networks = new();
    private readonly Dictionary<string, IVolume> volumes = new();
    private IContainer arServerContainer = null!;
    private readonly string testRunId;
    private readonly string arServerContainerName;
    private readonly Dictionary<string, int> randomPorts = new();

    public string Host => "localhost";
    public ushort Port { get; }

    public Uri Uri => new($"ws://{Host}:{Port}");

    public Arcor2ServerFixture() {
        testRunId = GenerateRunId();
        arServerContainerName = $"fit-demo-arserver-{testRunId}";

        Port = GetRandomPort();
    }

    public async Task InitializeAsync() {
        await CreateNetworksAsync();
        await CreateVolumesAsync();
        await StartContainersAsync();
        await WaitForServerReadyAsync();
    }

    public async Task DisposeAsync() {
        await arServerContainer.StopAsync();

        var stopTasks = containers.Where(c => c != arServerContainer).Select(container => container.StopAsync()).ToList();
        await Task.WhenAll(stopTasks);
        var disposeTasks = containers.Select(container => container.DisposeAsync().AsTask()).ToList();
        await Task.WhenAll(disposeTasks);

        foreach(var volume in volumes.Values) {
            await volume.DeleteAsync();
        }

        foreach(var network in networks.Values) {
            await network.DeleteAsync();
        }
    }

    private static string GenerateRunId() => Guid.NewGuid().ToString()[..8];

    private ushort GetRandomPort() {
        // Find an available port in the dynamic/private port range (49152-65535)
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint) socket.LocalEndPoint!)!.Port;

        return (ushort) port;
    }

    private int GetServicePort(string serviceName) {
        if(!randomPorts.ContainsKey(serviceName)) {
            randomPorts[serviceName] = GetRandomPort();
        }
        return randomPorts[serviceName];
    }

    private async Task CreateNetworksAsync() {
        string[] networkNames = [
            "scene", "execution", "project", "asset",
            "dobot-magician", "dobot-magician2", "dobot-m1", "calibration"
        ];

        foreach(var name in networkNames) {
            var network = new NetworkBuilder()
                .WithName($"fit-demo-{name}-network-{testRunId}")
                .Build();

            networks.Add(name, network);
            await network.CreateAsync();
        }
    }

    private async Task CreateVolumesAsync() {
        string[] volumeNames = ["asset", "execution"];

        foreach(var name in volumeNames) {
            var volume = new VolumeBuilder()
                .WithName($"fit-demo-{name}-{testRunId}")
                .Build();

            volumes.Add(name, volume);
            await volume.CreateAsync();
        }
    }

    private async Task StartContainersAsync() {
        // Asset Container
        var assetPort = GetServicePort("asset");
        var assetContainer = new ContainerBuilder()
            .WithImage("registry.gitlab.com/kinalisoft/test-it-off/asset:2.0.2")
            .WithName($"fit-demo-asset-{testRunId}")
            .WithEnvironment("ASSETS_FOLDER", "/tmp/assets")
            .WithPortBinding(assetPort.ToString(), "10040")
            .WithNetwork(networks["asset"])
            .WithNetworkAliases("fit-demo-asset")
            .WithVolumeMount(volumes["asset"], "/tmp/assets")
            .Build();

        containers.Add(assetContainer);
        await assetContainer.StartAsync();

        // Project Container
        var projectPort1 = GetServicePort("project1");
        var projectPort2 = GetServicePort("project2");
        var projectContainer = new ContainerBuilder()
            .WithImage("registry.gitlab.com/kinalisoft/test-it-off/project:2.0.2")
            .WithName($"fit-demo-project-{testRunId}")
            .WithEnvironment("ASSET_SERVICE_URL", "http://fit-demo-asset:10040")
            .WithPortBinding(projectPort1.ToString(), "10000")
            .WithPortBinding(projectPort2.ToString(), "10001")
            .WithNetwork(networks["project"])
            .WithNetwork(networks["asset"])
            .WithNetworkAliases("fit-demo-project")
            .Build();

        containers.Add(projectContainer);
        await projectContainer.StartAsync();

        // Scene Container
        var scenePort = GetServicePort("scene");
        var sceneContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_scene:1.1.0")
            .WithName($"fit-demo-scene-{testRunId}")
            .WithPortBinding(scenePort.ToString(), "5013")
            .WithNetwork(networks["scene"])
            .WithNetworkAliases("fit-demo-scene")
            .Build();

        containers.Add(sceneContainer);
        await sceneContainer.StartAsync();

        // Build Container
        var buildPort = GetServicePort("build");
        var buildContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_build:1.7.0")
            .WithName($"fit-demo-build-{testRunId}")
            .WithEnvironment("ARCOR2_PROJECT_SERVICE_URL", "http://fit-demo-project:10000")
            .WithEnvironment("ARCOR2_PROJECT_PATH", "")
            .WithPortBinding(buildPort.ToString(), "5008")
            .WithNetwork(networks["project"])
            .WithNetworkAliases("fit-demo-build")
            .Build();

        containers.Add(buildContainer);
        await buildContainer.StartAsync();

        // Execution Container
        var executionPort = GetServicePort("execution");
        var executionContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_execution:1.6.0")
            .WithName($"fit-demo-execution-{testRunId}")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_PROJECT_PATH", "/root/project")
            .WithPortBinding(executionPort.ToString(), "6790")
            .WithNetwork(networks["execution"])
            .WithNetwork(networks["scene"])
            .WithNetwork(networks["dobot-magician"])
            .WithNetwork(networks["dobot-magician2"])
            .WithNetwork(networks["dobot-m1"])
            .WithNetworkAliases("fit-demo-execution")
            .WithVolumeMount(volumes["execution"], "/root/project")
            .Build();

        containers.Add(executionContainer);
        await executionContainer.StartAsync();

        // Execution Proxy Container
        var executionProxyPort = GetServicePort("execution-proxy");
        var executionProxyContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_execution_proxy:1.2.1")
            .WithName($"fit-demo-execution-proxy-{testRunId}")
            .WithEnvironment("ARCOR2_PROJECT_PATH", "/root/project")
            .WithEnvironment("ARCOR2_EXECUTION_URL", "ws://fit-demo-execution:6790")
            .WithPortBinding(executionProxyPort.ToString(), "5009")
            .WithNetwork(networks["execution"])
            .WithNetworkAliases("fit-demo-execution-proxy")
            .WithVolumeMount(volumes["execution"], "/root/project")
            .Build();

        containers.Add(executionProxyContainer);
        await executionProxyContainer.StartAsync();

        // Calibration Container
        var calibrationPort = GetServicePort("calibration");
        var calibrationContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_calibration:1.2.0")
            .WithName($"fit-demo-calibration-{testRunId}")
            .WithEnvironment("ARCOR2_CALIBRATION_URL", "http://fit-demo-calibration:5014")
            .WithEnvironment("ARCOR2_CALIBRATION_MOCK", "false")
            .WithPortBinding(calibrationPort.ToString(), "5014")
            .WithNetwork(networks["calibration"])
            .WithNetworkAliases("fit-demo-calibration")
            .Build();

        containers.Add(calibrationContainer);
        await calibrationContainer.StartAsync();

        // Dobot Magician Container
        var dobotMagicianPort = GetServicePort("dobot-magician");
        var dobotMagicianContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_dobot:1.3.0")
            .WithName($"fit-demo-dobot-magician-{testRunId}")
            .WithEnvironment("ARCOR2_DOBOT_MOCK", "true")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_DOBOT_MODEL", "magician")
            .WithPortBinding(dobotMagicianPort.ToString(), "5018")
            .WithNetwork(networks["dobot-magician"])
            .WithNetwork(networks["scene"])
            .WithNetworkAliases("fit-demo-dobot-magician")
            .Build();

        containers.Add(dobotMagicianContainer);
        await dobotMagicianContainer.StartAsync();

        // Dobot Magician2 Container
        var dobotMagician2Port = GetServicePort("dobot-magician2");
        var dobotMagician2Container = new ContainerBuilder()
            .WithImage("arcor2/arcor2_dobot:1.3.0")
            .WithName($"fit-demo-dobot-magician2-{testRunId}")
            .WithEnvironment("ARCOR2_DOBOT_MOCK", "true")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_DOBOT_MODEL", "magician")
            .WithPortBinding(dobotMagician2Port.ToString(), "5018")
            .WithNetwork(networks["dobot-magician2"])
            .WithNetwork(networks["scene"])
            .WithNetworkAliases("fit-demo-dobot-magician2")
            .Build();

        containers.Add(dobotMagician2Container);
        await dobotMagician2Container.StartAsync();

        // Dobot M1 Container
        var dobotM1Port = GetServicePort("dobot-m1");
        var dobotM1Container = new ContainerBuilder()
            .WithImage("arcor2/arcor2_dobot:1.3.0")
            .WithName($"fit-demo-dobot-m1-{testRunId}")
            .WithEnvironment("ARCOR2_DOBOT_MOCK", "true")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_DOBOT_MODEL", "m1")
            .WithPortBinding(dobotM1Port.ToString(), "5018")
            .WithNetwork(networks["dobot-m1"])
            .WithNetwork(networks["scene"])
            .WithNetworkAliases("fit-demo-dobot-m1")
            .Build();

        containers.Add(dobotM1Container);
        await dobotM1Container.StartAsync();

        // Nginx Container
        var nginxPort = GetServicePort("nginx");
        var nginxContainer = new ContainerBuilder()
            .WithImage("nginx:1.27.1")
            .WithName($"fit-demo-nginx-{testRunId}")
            .WithPortBinding(nginxPort.ToString(), "80")
            .WithNetwork(networks["asset"])
            .WithNetworkAliases("fit-demo-nginx")
            .Build();

        containers.Add(nginxContainer);
        await nginxContainer.StartAsync();

        // Upload Object Types Container
        var uploadObjectTypesContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_upload_fit_demo:1.5.0")
            .WithName($"fit-demo-upload-object-types-{testRunId}")
            .WithEnvironment("ARCOR2_PROJECT_SERVICE_URL", "http://fit-demo-project:10000")
            .WithEnvironment("ARCOR2_ASSET_SERVICE_URL", "http://fit-demo-asset:10040")
            .WithNetwork(networks["project"])
            .WithNetwork(networks["asset"])
            .WithNetworkAliases("fit-demo-upload-object-types")
            .Build();

        containers.Add(uploadObjectTypesContainer);
        await uploadObjectTypesContainer.StartAsync();

        // Upload Builtin Objects Container
        var uploadBuiltinObjectsContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_upload_builtin_objects:1.3.0")
            .WithName($"fit-demo-upload-builtin-objects-{testRunId}")
            .WithEnvironment("ARCOR2_PROJECT_SERVICE_URL", "http://fit-demo-project:10000")
            .WithEnvironment("ARCOR2_ASSET_SERVICE_URL", "http://fit-demo-asset:10040")
            .WithNetwork(networks["project"])
            .WithNetwork(networks["asset"])
            .WithNetworkAliases("fit-demo-upload-builtin-objects")
            .Build();

        containers.Add(uploadBuiltinObjectsContainer);
        await uploadBuiltinObjectsContainer.StartAsync();

        // TODO: Change
        await Task.Delay(8000);


        // AR Server Container - Start this last as it depends on all other services
        var secondaryServerPort = GetServicePort("secondary-server");
        arServerContainer = new ContainerBuilder()
            .WithImage("arcor2/arcor2_arserver:1.3.1")
            .WithName(arServerContainerName)
            .WithEnvironment("ARCOR2_PROJECT_SERVICE_URL", "http://fit-demo-project:10000")
            .WithEnvironment("ARCOR2_ASSET_SERVICE_URL", "http://fit-demo-asset:10040")
            .WithEnvironment("ARCOR2_SCENE_SERVICE_URL", "http://fit-demo-scene:5013")
            .WithEnvironment("ARCOR2_EXECUTION_URL", "ws://fit-demo-execution:6790")
            .WithEnvironment("ARCOR2_BUILD_URL", "http://fit-demo-build:5008")
            .WithEnvironment("ARCOR2_CALIBRATION_URL", "http://fit-demo-calibration:5014")
            .WithPortBinding(Port.ToString(), "6789")
            .WithPortBinding(secondaryServerPort.ToString(), "6799")
            .WithNetwork(networks["scene"])
            .WithNetwork(networks["project"])
            .WithNetwork(networks["asset"])
            .WithNetwork(networks["dobot-magician"])
            .WithNetwork(networks["dobot-magician2"])
            .WithNetwork(networks["dobot-m1"])
            .WithNetwork(networks["calibration"])
            .WithNetworkAliases("fit-demo-arserver")
            .Build();

        containers.Add(arServerContainer);

        await arServerContainer.StartAsync();
    }

    private async Task WaitForServerReadyAsync() {
        const int maxRetries = 90;
        const int delayMs = 1000;

        for(int i = 0; i < maxRetries; i++) {
            try {
                using var client = new ClientWebSocket();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(Uri, cts.Token);
                if(client.State == WebSocketState.Open) {
                    return;
                }
            }
            catch { }

            await Task.Delay(delayMs);
        }

        throw new TimeoutException($"Server at {Uri} did not become ready within {maxRetries * delayMs / 1000} seconds");
    }
}