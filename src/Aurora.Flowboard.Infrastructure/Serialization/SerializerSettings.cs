namespace Aurora.Flowboard.Infrastructure.Serialization;

internal sealed class SerializerSettings
{
    internal static readonly JsonSerializerSettings Instance = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        SerializationBinder = new DomainEventSerializationBinder(),
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
}
