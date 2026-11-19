using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace NixDaemonProxy.Client;


[Command("status")]
partial class CurrentCommand : ICommand
{
    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var client = new NixDaemonProxyClient(ControlSocket);
        var fingerprint = await client.CurrentAsync();
        console.WriteLine(client.Serialize(fingerprint));
    }
}