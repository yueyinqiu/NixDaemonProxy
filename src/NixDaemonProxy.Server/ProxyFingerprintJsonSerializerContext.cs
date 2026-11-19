using System.Text.Json.Serialization;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Server;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProxyFingerprint))]
partial class ProxyFingerprintJsonSerializerContext : JsonSerializerContext
{
}