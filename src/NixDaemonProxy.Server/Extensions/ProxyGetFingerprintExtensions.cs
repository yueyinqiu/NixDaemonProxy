using System.Security.Cryptography;
using System.Text.Json;
using NixDaemonProxy.Interface;
using NixDaemonProxy.Server;
using Titanium.Web.Proxy.Models;

static class ProxyGetFingerprintExtensions
{
    public static string GetFingerprint(this Proxy proxy)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            proxy,
            ProxyJsonSerializerContext.Default.Proxy
        );
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}