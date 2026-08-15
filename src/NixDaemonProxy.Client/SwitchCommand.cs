using System.Net.Http.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using CliWrap;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Server;


[Command("switch")]
public partial class HttpCommand : ICommand
{
    [CommandOption("host-name", 'H')]
    public required string HostName { get; set; }

    [CommandOption("port", 'P')]
    public required int Port { get; set; }

    [CommandOption("user-name", 'u')]
    public string? UserName { get; set; } = null;

    [CommandOption("password", 'p')]
    public string? Password { get; set; } = null;

    [CommandOption("proxy-dns-requests")]
    public bool ProxyDnsRequests { get; set; } = true;

    [CommandOption("bypass-localhost")]
    public bool BypassLocalhost { get; set; } = false;
    
    [CommandOption("control-socket")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var proxy = new Proxy(
            ProxyType.Http,
            this.HostName, this.Port,
            this.UserName, this.Password,
            this.ProxyDnsRequests, this.BypassLocalhost, null
        );
        using var client = UnixSocketHttpClient.Create(this.ControlSocket);
        var response = await client.PostAsJsonAsync("switch", proxy);
        Console.WriteLine(response.StatusCode);
    }
}