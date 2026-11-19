using System.Text;
using System.Text.Json;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;

static class ProxyStatusExtensions
{
    private static string ToText(this ProxyStatus status)
    {
        var builder = new StringBuilder();
        if (status.Type is null)
            builder.Append("Direct");
        else
            builder.Append(status.Type);
        if (status.Fingerprint is not null)
        {
            builder.Append(':');
            builder.Append(status.Fingerprint);
        }
        builder.AppendLine();

        builder.AppendLine(status.Comment);
        return builder.ToString();
    }

    private static string ToJson(this ProxyStatus status)
    {
        return JsonSerializer.Serialize(
            status,
            ProxyStatusJsonSerializerContext.Default.ProxyStatus
        );
    }

    public static void WriteProxyStatus(this IConsole console, ProxyStatus status, bool json)
    {
        console.Output.WriteLine(json ? status.ToJson() : status.ToText());
    }
}