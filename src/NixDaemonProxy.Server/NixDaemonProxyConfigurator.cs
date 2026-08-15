using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;

namespace NixDaemonProxy.Server;

class NixDaemonProxyConfigurator(string serviceName)
{
    private readonly FileInfo file = new(
        $"/run/systemd/system/{serviceName}.service.d/nix-daemon-proxy-02f2de3ae7134c999a969d2b8f6f2f46.conf"
    );

    private string GetContent(string? proxy)
    {
        if (proxy is null)
        {
            return "";
        }
        return
            $"""
            [Service]
            Environment="all_proxy={proxy}"
            Environment="http_proxy={proxy}"
            Environment="https_proxy={proxy}"
            Environment="ALL_PROXY={proxy}"
            Environment="HTTP_PROXY={proxy}"
            Environment="HTTPS_PROXY={proxy}"
            """;
    }

    public async Task UpdateAsync(string? proxy)
    {
        Debug.Assert(file.Directory is not null);
        file.Directory.Create();
        await File.WriteAllTextAsync(file.FullName, GetContent(proxy));
    }

    public void Update(string? proxy)
    {
        Debug.Assert(file.Directory is not null);
        file.Directory.Create();
        File.WriteAllText(file.FullName, GetContent(proxy));
    }

    public async Task RestartServiceAsync()
    {
        await Cli.Wrap("systemctl").WithArguments(["daemon-reload"]).ExecuteBufferedAsync();
        await Cli.Wrap("systemctl").WithArguments(["restart", serviceName]).ExecuteBufferedAsync();
    }
}