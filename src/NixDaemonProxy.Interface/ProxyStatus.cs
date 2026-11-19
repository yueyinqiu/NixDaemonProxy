namespace NixDaemonProxy.Interface;

public sealed record ProxyStatus(
    string? Fingerprint,
    ProxyType? Type,
    string Comment
);