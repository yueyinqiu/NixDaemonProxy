using System.Net.Http.Json;
using System.Text.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;


[Command("from-json")]
partial class FromJsonCommand : ICommand
{
    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    [CommandOption("dry-run", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_DRY_RUN")]
    public bool DryRun { get; set; } = false;

    [CommandOption("json", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_JSON")]
    public bool Json { get; set; } = false;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var proxy = await JsonSerializer.DeserializeAsync(
            console.Input.BaseStream,
            CommentedProxyJsonSerializerContext.Default.CommentedProxy
        );
        var client = new NixDaemonProxyClient(ControlSocket);
        var config = proxy ?? new CommentedProxy(null, "");
        var status = await (DryRun ?
            client.FingerprintAsync(config) :
            client.SwitchAsync(config)
        );
        console.WriteProxyStatus(status, this.Json);
    }
}
