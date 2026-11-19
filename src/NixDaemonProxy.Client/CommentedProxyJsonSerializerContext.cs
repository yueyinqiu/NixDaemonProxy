using System.Text.Json.Serialization;
using NixDaemonProxy.Interface;

namespace NixDaemonProxy.Client;

[JsonSourceGenerationOptions(RespectNullableAnnotations = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CommentedProxy))]
partial class CommentedProxyJsonSerializerContext : JsonSerializerContext
{
}