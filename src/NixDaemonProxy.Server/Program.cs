using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Mvc;
using NixDaemonProxy.Interface;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace NixDaemonProxy.Server;


[Command]
public partial class Program : ICommand
{
    [CommandOption("control-socket")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    [CommandOption("control-group")]
    public string ControlGroup { get; set; } = "nix-daemon-proxy";

    [CommandOption("proxy-port")]
    public int ProxyPort { get; set; } = 0;

    [CommandOption("nix-daemon-service")]
    public string? NixDaemonService { get; set; } = "nix-daemon";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var password = Guid.NewGuid().ToString("N");

        using var proxyServer = new ProxyServer(false);
        proxyServer.AddEndPoint(new ExplicitProxyEndPoint(new IPAddress([127, 0, 0, 1]), this.ProxyPort, false));
        proxyServer.ProxyBasicAuthenticateFunc = async (_, _, x) => x == password;
        proxyServer.Start(false);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenUnixSocket(this.ControlSocket);
        });
        var app = builder.Build();

        app.MapPost("/switch", async ([FromBody] Proxy proxy) =>
        {
            var externalProxy = proxy.ToExternalProxy();
            proxyServer.UpStreamHttpProxy = externalProxy;
            proxyServer.UpStreamHttpsProxy = externalProxy;
        });

        var daemonConfigurator = NixDaemonService is null ? null : new NixDaemonProxyConfigurator(NixDaemonService);
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            if (ControlGroup is not null)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                File.SetUnixFileMode(this.ControlSocket,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite);

                Cli.Wrap("chown").WithArguments([
                    $":{this.ControlGroup}", this.ControlSocket
                ]).ExecuteBufferedAsync().Task.Wait();
            }

            daemonConfigurator?.Update(
                $"http://user:{password}@127.0.0.1:{proxyServer.ProxyEndPoints[0].Port}/"
            );
            daemonConfigurator?.RestartServiceAsync().Wait();
        });

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            daemonConfigurator?.Update(null);
            daemonConfigurator?.RestartServiceAsync().Wait();
        });

        app.Run();
    }
}