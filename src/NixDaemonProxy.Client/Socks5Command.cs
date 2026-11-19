using System.Net.Http.Json;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;


[Command("socks5")]
partial class Socks5Command : ICommand
{
    [CommandOption("host-name", 'H', EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_HOST_NAME")]
    public required string HostName { get; set; }

    [CommandOption("port", 'P', EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_PORT")]
    public required int Port { get; set; }

    [CommandOption("user-name", 'u', EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_USER_NAME")]
    public string? UserName { get; set; } = null;

    [CommandOption("password", 'p', EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_PASSWORD")]
    public string? Password { get; set; } = null;

    [CommandOption("proxy-dns-requests", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_PROXY_DNS_REQUESTS")]
    public bool ProxyDnsRequests { get; set; } = true;

    [CommandOption("bypass-localhost", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_BYPASS_LOCALHOST")]
    public bool BypassLocalhost { get; set; } = false;

    [CommandOption("comment", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_COMMENT")]
    public string Comment { get; set; } = "";

    [CommandOption("dry-run", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_DRY_RUN")]
    public bool DryRun { get; set; } = false;
    
    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var proxy = new Proxy(
            this.Comment,
            ProxyType.Socks5,
            this.HostName, this.Port,
            this.UserName, this.Password,
            this.ProxyDnsRequests, this.BypassLocalhost, null
        );
        var client = new NixDaemonProxyClient(ControlSocket);
        var fingerprint = await (DryRun ?
            client.FingerprintAsync(proxy) :
            client.SwitchAsync(proxy)
        );
        console.WriteLine(client.Serialize(fingerprint));
    }
}