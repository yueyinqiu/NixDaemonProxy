using System.Net.Http.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace NixDaemonProxy.Client;


[Command("direct")]
partial class DirectCommand : ICommand
{
    [CommandOption("control-socket")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        using var client = UnixSocketHttpClient.Create(this.ControlSocket);
        var response = await client.PostAsJsonAsync("switch", null, ProxyJsonSerializerContext.Default.Proxy);
        response.EnsureSuccessStatusCode();
        console.WriteLine(response.StatusCode);
    }
}