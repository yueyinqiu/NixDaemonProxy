using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace NixDaemonProxy.Client;

[Command("status")]
partial class StatusCommand : ICommand
{
    [CommandOption("json", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_JSON")]
    public bool Json { get; set; } = false;

    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var client = new NixDaemonProxyClient(ControlSocket);
        var status = await client.StatusAsync();
        console.WriteProxyStatus(status, this.Json);
    }
}