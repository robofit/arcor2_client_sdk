using Arcor2.ClientSdk.Communication;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;

public class TestLogger(ITestOutputHelper output) : IArcor2Logger {
    public void LogInfo(string message) {
        output.WriteLine($"INFO: {message}");
    }

    public void LogError(string message) {
        output.WriteLine($"ERR: {message}");
    }

    public void LogWarn(string message) {
        output.WriteLine($"WARN: {message}");
    }
}