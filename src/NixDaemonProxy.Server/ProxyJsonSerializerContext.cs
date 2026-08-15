using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Server;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Proxy))]
partial class ProxyJsonSerializerContext : JsonSerializerContext
{
}