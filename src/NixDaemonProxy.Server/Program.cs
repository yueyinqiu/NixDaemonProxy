using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    [CommandOption("control-socket", EnvironmentVariable = "NIX_DAEMON_PROXY_SERVER_ARGUMENT_CONTROL_SOCKET")]
    public string ControlSocket { get; set; } = "/run/nix-daemon-proxy.sock";

    [CommandOption("control-group", EnvironmentVariable = "NIX_DAEMON_PROXY_SERVER_ARGUMENT_CONTROL_GROUP")]
    public string ControlGroup { get; set; } = "nix-daemon-proxy";

    [CommandOption("proxy-password", EnvironmentVariable = "NIX_DAEMON_PROXY_SERVER_ARGUMENT_PROXY_PASSWORD")]
    public string? ProxyPassword { get; set; } = null;

    [CommandOption("proxy-port", EnvironmentVariable = "NIX_DAEMON_PROXY_SERVER_ARGUMENT_PROXY_PORT")]
    public int ProxyPort { get; set; } = 0;

    [CommandOption("nix-daemon-service", EnvironmentVariable = "NIX_DAEMON_PROXY_SERVER_ARGUMENT_NIX_DAEMON_SERVICE")]
    public string? NixDaemonService { get; set; } = "nix-daemon";

    private readonly Lock locker = new();
    private ProxyFingerprint currentProxy = GetFingerprint(null);

    private static ProxyFingerprint GetFingerprint(Proxy? proxy)
    {
        if (proxy is null)
        {
            return new ProxyFingerprint("Direct", "");
        }
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            proxy,
            ProxyJsonSerializerContext.Default.Proxy
        );
        return new ProxyFingerprint(
            $"{proxy.ProxyType}:{Convert.ToHexString(SHA256.HashData(bytes))}",
            proxy.Comment
        );
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var password = this.ProxyPassword ?? Guid.NewGuid().ToString("N");

        using var proxyServer = new ProxyServer(false);
        proxyServer.AddEndPoint(new ExplicitProxyEndPoint(new IPAddress([127, 0, 0, 1]), this.ProxyPort, false));
        proxyServer.ProxyBasicAuthenticateFunc = async (_, _, x) => x == password;
        proxyServer.Start(false);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrel();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenUnixSocket(this.ControlSocket);
        });
        builder.Services.AddRouting();
        builder.Services.ConfigureHttpJsonOptions(x =>
        {
            x.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(
                ProxyJsonSerializerContext.Default,
                ProxyFingerprintJsonSerializerContext.Default
            );
        });
        var app = builder.Build();


        app.MapPost("/switch", ([FromBody] Proxy? proxy) =>
        {
            var externalProxy = proxy?.ToExternalProxy();
            var fingerprint = GetFingerprint(proxy);
            lock (this.locker)
            {
                proxyServer.UpStreamHttpProxy = externalProxy;
                proxyServer.UpStreamHttpsProxy = externalProxy;
                this.currentProxy = fingerprint;
            }
            return fingerprint;
        });

        app.MapPost("/current", () => this.currentProxy);
        app.MapPost("/fingerprint", ([FromBody] Proxy? proxy) => GetFingerprint(proxy));

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