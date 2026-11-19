using System.Net.Http.Json;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;

internal sealed class NixDaemonProxyClient(string controlSocket)
{
    public async Task<ProxyStatus> SwitchAsync(
        CommentedProxy proxy,
        CancellationToken cancellationToken = default
    )
    {
        using var client = UnixSocketHttpClient.Create(controlSocket);
        using var content = JsonContent.Create(proxy, CommentedProxyJsonSerializerContext.Default.CommentedProxy);
        using var response = await client.PostAsync("switch", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var fingerprint = await response.Content.ReadFromJsonAsync(
            ProxyFingerprintJsonSerializerContext.Default.ProxyFingerprint,
            cancellationToken
        ) ?? throw new InvalidOperationException("switch returned null");
        return new ProxyStatus(
            fingerprint.Fingerprint,
            proxy.Proxy?.ProxyType,
            proxy.Comment
        );
    }

    public async Task<ProxyStatus> FingerprintAsync(
        CommentedProxy proxy,
        CancellationToken cancellationToken = default
    )
    {
        using var client = UnixSocketHttpClient.Create(controlSocket);
        using var content = JsonContent.Create(proxy.Proxy, ProxyJsonSerializerContext.Default.Proxy);
        using var response = await client.PostAsync("fingerprint", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var fingerprint = await response.Content.ReadFromJsonAsync(
            ProxyFingerprintJsonSerializerContext.Default.ProxyFingerprint,
            cancellationToken
        ) ?? throw new InvalidOperationException("fingerprint returned null");
        return new ProxyStatus(
            fingerprint.Fingerprint,
            proxy.Proxy?.ProxyType,
            proxy.Comment
        );
    }

    public async Task<ProxyStatus> StatusAsync(CancellationToken cancellationToken = default)
    {
        using var client = UnixSocketHttpClient.Create(controlSocket);
        using var response = await client.PostAsync("status", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync(
            ProxyStatusJsonSerializerContext.Default.ProxyStatus,
            cancellationToken
        ) ?? throw new InvalidOperationException("status returned null");
        return result;
    }
}