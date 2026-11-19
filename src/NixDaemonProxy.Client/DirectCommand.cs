using System.Net.Http.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace NixDaemonProxy.Client;


[Command("direct")]
partial class DirectCommand : ICommand
{
    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var client = new NixDaemonProxyClient(ControlSocket);
        var fingerprint = await client.SwitchAsync(null);
        console.WriteLine(client.Serialize(fingerprint));
    }
}