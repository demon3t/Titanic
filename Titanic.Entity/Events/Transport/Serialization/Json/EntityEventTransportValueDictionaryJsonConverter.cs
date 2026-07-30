using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Преобразует словарь transport-значений событийного слоя между JSON и CLR-скалярами.
    /// </summary>
    internal sealed class EntityEventTransportValueDictionaryJsonConverter
        : JsonConverter<Dictionary<string, object?>>
    {
        #region Fields

        /// <summary>
        /// Конвертер одного scalar-значения, переиспользуемый для всех элементов словаря.
        /// </summary>
        private static readonly EntityEventTransportValueJsonConverter ValueConverter = new();

        #endregion Fields

        #region Members

        /// <inheritdoc />
        public override Dictionary<string, object?> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Entity event values dictionary must be a JSON object.");
            }

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return result;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Unexpected token inside entity event values dictionary.");
                }

                var key = reader.GetString()
                    ?? throw new JsonException("Dictionary key cannot be null.");

                reader.Read();
                result[key] = ValueConverter.Read(ref reader, typeof(object), options);
            }

            throw new JsonException("Unexpected end of JSON while reading entity event values dictionary.");
        }

        /// <inheritdoc />
        public override void Write(
            Utf8JsonWriter writer,
            Dictionary<string, object?> value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var item in value)
            {
                writer.WritePropertyName(item.Key);
                ValueConverter.Write(writer, item.Value, options);
            }

            writer.WriteEndObject();
        }

        #endregion Members
    }
}
