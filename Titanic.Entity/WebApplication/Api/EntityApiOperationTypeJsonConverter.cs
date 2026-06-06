using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// JSON-конвертер типа операции Entity API, который не роняет HTTP pipeline на неизвестном значении.
    /// </summary>
    public sealed class EntityApiOperationTypeJsonConverter : JsonConverter<EntityApiOperationType>
    {
        #region JsonConverter

        /// <inheritdoc />
        public override EntityApiOperationType Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                return Enum.TryParse<EntityApiOperationType>(value, ignoreCase: true, out var operation)
                    ? operation
                    : EntityApiOperationType.Unknown;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
            {
                return Enum.IsDefined(typeof(EntityApiOperationType), number)
                    ? (EntityApiOperationType)number
                    : EntityApiOperationType.Unknown;
            }

            return EntityApiOperationType.Unknown;
        }

        /// <inheritdoc />
        public override void Write(
            Utf8JsonWriter writer,
            EntityApiOperationType value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        #endregion JsonConverter
    }
}
