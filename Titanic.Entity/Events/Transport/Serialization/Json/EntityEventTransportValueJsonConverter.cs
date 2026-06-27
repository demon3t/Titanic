using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Преобразует transport-значения событийного слоя между JSON и CLR-скалярами.
    /// </summary>
    internal sealed class EntityEventTransportValueJsonConverter : JsonConverter<object?>
    {
        #region Members

        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number when reader.TryGetInt32(out var intValue) => intValue,
                JsonTokenType.Number when reader.TryGetInt64(out var longValue) => longValue,
                JsonTokenType.Number when reader.TryGetDecimal(out var decimalValue) => decimalValue,
                JsonTokenType.Number => reader.GetDouble(),
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case JsonElement element:
                    element.WriteTo(writer);
                    return;
                default:
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
                    return;
            }
        }

        #endregion Members
    }
}
