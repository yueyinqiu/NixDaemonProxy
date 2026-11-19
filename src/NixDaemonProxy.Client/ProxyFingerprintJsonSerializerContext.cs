using System.Text.Json.Serialization;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true, UseStringEnumConverter = true, WriteIndented = true)]
[JsonSerializable(typeof(ProxyFingerprint))]
partial class ProxyFingerprintJsonSerializerContext : JsonSerializerContext
{
}