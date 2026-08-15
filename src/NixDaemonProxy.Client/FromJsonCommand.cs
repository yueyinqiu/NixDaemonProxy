using System.Net.Http.Json;
using System.Text.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;


[Command("from-json")]
public partial class FromJsonCommand : ICommand
{
    [CommandOption("json", 'j')]
    public required string Json { get; set; }

    [CommandOption("control-socket")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var proxy = JsonSerializer.Deserialize<Proxy?>(this.Json);
        using var client = UnixSocketHttpClient.Create(this.ControlSocket);
        var response = await client.PostAsJsonAsync("switch", proxy);
        response.EnsureSuccessStatusCode();
        console.WriteLine(response.StatusCode);
    }
}