using NixDaemonProxy.Interface;
using Titanium.Web.Proxy.Models;

static class ProxyToExternalProxyExtensions
{
    public static ExternalProxy ToExternalProxy(this Proxy proxy)
    {
        return new ExternalProxy()
        {
            BypassLocalhost = proxy.BypassLocalhost,
            HostName = proxy.HostName,
            NextHop = proxy.NextHop is null ? null : ToExternalProxy(proxy.NextHop),
            Password = proxy.Password,
            Port = proxy.Port,
            ProxyDnsRequests = proxy.ProxyDnsRequests,
            ProxyType = proxy.ProxyType switch
            {
                ProxyType.Http => ExternalProxyType.Http,
                ProxyType.Socks4 => ExternalProxyType.Socks4,
                ProxyType.Socks5 => ExternalProxyType.Socks5,
                _ => (ExternalProxyType)(int)proxy.ProxyType
            },
            UseDefaultCredentials = false,
            UserName = proxy.UserName
        };
    }
}