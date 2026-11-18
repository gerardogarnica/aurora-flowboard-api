using Newtonsoft.Json.Serialization;
using System.Collections.Frozen;
using System.Reflection;

namespace Aurora.Flowboard.Infrastructure.Serialization;

// REMARK: Outbox content is written with TypeNameHandling, so deserializing it instantiates whatever
// "$type" says. This binder only resolves the concrete domain events of the Domain assembly, so a row
// written by anything else cannot make Json.NET instantiate an arbitrary type. Events are flat
// (Guid, string, enums, decimal): an event with a nested object or collection would carry a second
// "$type" that this binder rejects.
internal sealed class DomainEventSerializationBinder : ISerializationBinder
{
    private static readonly Assembly DomainAssembly = typeof(DomainEvent).Assembly;
    private static readonly string DomainAssemblyName = DomainAssembly.GetName().Name!;

    private static readonly FrozenDictionary<string, Type> AllowedTypes = DomainAssembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IDomainEvent)))
        .ToFrozenDictionary(type => type.FullName!, StringComparer.Ordinal);

    public Type BindToType(string? assemblyName, string typeName)
    {
        if (string.Equals(assemblyName, DomainAssemblyName, StringComparison.Ordinal) &&
            AllowedTypes.TryGetValue(typeName, out Type? type))
        {
            return type;
        }

        throw new JsonSerializationException(
            $"Type '{typeName}, {assemblyName}' is not an allowed domain event type.");
    }

    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        assemblyName = serializedType.Assembly.GetName().Name;
        typeName = serializedType.FullName;
    }
}
