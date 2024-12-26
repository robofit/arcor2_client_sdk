using Arcor2.ClientSdk.Communication;

namespace Ignore.Test;

internal class Program {
    static async Task Main(string[] args) {
        var arcor = new Arcor2Client();
        await arcor.ConnectAsync("hi.com", "25");
    }
}