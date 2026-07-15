using System.Text.Json;
using Titanic.Db.Enums;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Normalizes Entity values before they are applied to entities or SQL queries.
    /// </summary>
    internal static class EntityValueNormalizer
    {
        /// <summary>
        /// Converts incoming values to CLR types expected by the entity columns.
        /// </summary>
        /// <param name="values">The values keyed by entity column, property or alias name.</param>
        /// <param name="structure">The optional entity structure used to resolve column data types.</param>
        /// <returns>A dictionary with normalized values and case-insensitive keys.</returns>
        internal static Dictionary<string, object?> NormalizeValues(
            IReadOnlyDictionary<string, object?> values,
            EntityStructure? structure = null)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.ToDictionary(
                x => x.Key,
                x => NormalizeColumnValue(FindColumnStructure(structure, x.Key), NormalizeJsonValue(x.Value)),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts a single value to the CLR type expected by the specified column.
        /// </summary>
        /// <param name="column">The optional column metadata that defines the target data type.</param>
        /// <param name="value">The incoming value from code, transport or JSON.</param>
        /// <returns>The normalized value, or the original value when no conversion is required.</returns>
        internal static object? NormalizeColumnValue(ColumnStructure? column, object? value)
        {
            value = NormalizeJsonValue(value);

            if (column?.DataValueType == DataValueType.Guid
                && value is string stringValue
                && Guid.TryParse(stringValue, out var guidValue))
            {
                return guidValue;
            }

            return value;
        }

        /// <summary>
        /// Converts a <see cref="JsonElement"/> value to a CLR primitive and leaves other values unchanged.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <returns>A CLR primitive for JSON values, or the original value for non-JSON values.</returns>
        internal static object? NormalizeJsonValue(object? value)
        {
            if (value is JsonElement element)
            {
                return NormalizeJsonElement(element);
            }

            return value;
        }

        /// <summary>
        /// Finds a column by property name, database column name or known alias.
        /// </summary>
        /// <param name="structure">The optional entity structure to search in.</param>
        /// <param name="columnName">The incoming column, property or alias name.</param>
        /// <returns>The matched column structure, or <see langword="null"/> when it cannot be resolved.</returns>
        private static ColumnStructure? FindColumnStructure(EntityStructure? structure, string columnName)
        {
            return structure?.ColumnsStructure.FirstOrDefault(x => x.Matches(columnName));
        }

        /// <summary>
        /// Converts a JSON value from <see cref="System.Text.Json"/> to the closest CLR primitive.
        /// </summary>
        /// <param name="element">The JSON element to convert.</param>
        /// <returns>The converted primitive value.</returns>
        /// <exception cref="NotSupportedException">Thrown when the JSON value kind is not supported by Entity values.</exception>
        private static object? NormalizeJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
                JsonValueKind.Number => element.GetDouble(),
                _ => throw new NotSupportedException($"JSON value kind '{element.ValueKind}' is not supported in Entity values.")
            };
        }
    }
}
