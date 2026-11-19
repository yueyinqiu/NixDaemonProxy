using System.Text.Json.Serialization;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Server;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProxyStatus))]
partial class ProxyStatusJsonSerializerContext : JsonSerializerContext
{
}