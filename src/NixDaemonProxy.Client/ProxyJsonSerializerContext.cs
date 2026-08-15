using System.Text.Json.Serialization;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Proxy))]
partial class ProxyJsonSerializerContext : JsonSerializerContext
{
}