using Newtonsoft.Json;

namespace Aurora.Flowboard.Domain.Abstractions;

public sealed class SerializerSettings
{
    public static readonly JsonSerializerSettings Instance = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
}