using System.Net.Http.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;


[Command("socks5")]
partial class Socks5Command : ICommand
{
    [CommandOption("host-name", 'H')]
    public required string HostName { get; set; }

    [CommandOption("port", 'P')]
    public required int Port { get; set; }

    [CommandOption("user-name", 'u')]
    public string? UserName { get; set; } = null;

    [CommandOption("password", 'p', EnvironmentVariable = "NIX_DAEMON_PROXY_CLIENT_SECRET_ARGUMENTS_PASSWORD")]
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
            ProxyType.Socks5,
            this.HostName, this.Port,
            this.UserName, this.Password,
            this.ProxyDnsRequests, this.BypassLocalhost, null
        );
        using var client = UnixSocketHttpClient.Create(this.ControlSocket);
        var response = await client.PostAsJsonAsync("switch", proxy, ProxyJsonSerializerContext.Default.Proxy);
        response.EnsureSuccessStatusCode();
        console.WriteLine(response.StatusCode);
    }
}