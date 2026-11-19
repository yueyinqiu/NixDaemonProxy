using System.Net.Http.Json;
using System.Text.Json;
using CliFx;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;

internal sealed class NixDaemonProxyClient(string controlSocket)
{
    public async Task<ProxyFingerprint?> SwitchAsync(
        Proxy? proxy,
        CancellationToken cancellationToken = default
    )
    {
        using var client = UnixSocketHttpClient.Create(controlSocket);
        using var content = JsonContent.Create(proxy, ProxyJsonSerializerContext.Default.Proxy);
        using var response = await client.PostAsync("switch", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            ProxyFingerprintJsonSerializerContext.Default.ProxyFingerprint,
            cancellationToken
        );
    }

    public async Task<ProxyFingerprint?> FingerprintAsync(
        Proxy? proxy,
        CancellationToken cancellationToken = default
    )
    {
        using var client = UnixSocketHttpClient.Create(controlSocket);
        using var content = JsonContent.Create(proxy, ProxyJsonSerializerContext.Default.Proxy);
        using var response = await client.PostAsync("switch", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            ProxyFingerprintJsonSerializerContext.Default.ProxyFingerprint,
            cancellationToken
        );
    }

    public async Task<ProxyFingerprint?> CurrentAsync(CancellationToken cancellationToken = default)
    {
        using var client = UnixSocketHttpClient.Create(controlSocket);
        using var response = await client.PostAsync("current", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(
            ProxyFingerprintJsonSerializerContext.Default.ProxyFingerprint,
            cancellationToken
        );
    }

    public string Serialize(ProxyFingerprint? status)
    {
        return JsonSerializer.Serialize(
            status,
            ProxyFingerprintJsonSerializerContext.Default.ProxyFingerprint
        );
    }
}