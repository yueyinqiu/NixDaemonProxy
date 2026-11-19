namespace NixDaemonProxy.Interface;

public sealed record Proxy(
    ProxyType ProxyType,
    string HostName,
    int Port,
    string? UserName,
    string? Password,
    bool ProxyDnsRequests,
    bool BypassLocalhost,
    Proxy? NextHop
);
