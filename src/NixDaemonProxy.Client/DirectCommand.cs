using System.Net.Http.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;


[Command("direct")]
partial class DirectCommand : ICommand
{
    [CommandOption("comment", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_COMMENT")]
    public string Comment { get; set; } = "";

    [CommandOption("json", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_JSON")]
    public bool Json { get; set; } = false;

    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var client = new NixDaemonProxyClient(ControlSocket);
        var status = await client.SwitchAsync(new CommentedProxy(null, this.Comment));
        console.WriteProxyStatus(status, this.Json);
    }
}