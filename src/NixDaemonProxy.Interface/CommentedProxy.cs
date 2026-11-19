namespace NixDaemonProxy.Interface;

public sealed record CommentedProxy(
    Proxy? Proxy,
    string Comment
);