using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Serialisation.Default;

public class DefaultEntityIdJsonConverter<TEntity> : JsonConverter<EntityId<TEntity>>
    where TEntity : Entity<TEntity>
{
    public override EntityId<TEntity> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Guid? guid = JsonSerializer.Deserialize<Guid>(ref reader, options);
        
        return guid is null ? EntityId<TEntity>.Uninitialised : new EntityId<TEntity>(guid.Value);
    }

    public override void Write(Utf8JsonWriter writer, EntityId<TEntity> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Value, options);
}