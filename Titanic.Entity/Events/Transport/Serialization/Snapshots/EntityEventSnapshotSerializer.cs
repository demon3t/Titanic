using System.Text.Json;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Сериализует и нормализует transport-снимок ORM-сущности для gRPC и HTTP.
    /// </summary>
    internal static class EntityEventSnapshotSerializer
    {
        #region Fields

        /// <summary>
        /// Общие JSON-настройки для сериализации и десериализации transport-снимков.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion Fields

        #region Members

        /// <summary>
        /// Сериализует снимок сущности в JSON-строку для gRPC transport-а.
        /// </summary>
        /// <param name="snapshot">Снимок сущности.</param>
        /// <returns>JSON-строка снимка или пустая строка.</returns>
        internal static string Serialize(EntityEventEntitySnapshot? snapshot)
        {
            return snapshot == null
                ? string.Empty
                : JsonSerializer.Serialize(snapshot, JsonOptions);
        }

        /// <summary>
        /// Десериализует transport-снимок и приводит его значения к CLR-типам.
        /// </summary>
        /// <param name="json">JSON-строка снимка.</param>
        /// <returns>Нормализованный снимок сущности или <see langword="null" />.</returns>
        internal static EntityEventEntitySnapshot? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var snapshot = JsonSerializer.Deserialize<EntityEventEntitySnapshot>(json, JsonOptions);
            return Normalize(snapshot);
        }

        /// <summary>
        /// Нормализует снимок, полученный по HTTP JSON transport-у.
        /// </summary>
        /// <param name="snapshot">Исходный снимок.</param>
        /// <returns>Нормализованный снимок или <see langword="null" />.</returns>
        internal static EntityEventEntitySnapshot? Normalize(EntityEventEntitySnapshot? snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            snapshot.Paths = snapshot.Paths?.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            snapshot.Columns = snapshot.Columns?.ToDictionary(
                x => x.Key,
                x => NormalizeColumn(x.Key, x.Value),
                StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, EntityEventColumnSnapshot>(StringComparer.OrdinalIgnoreCase);

            snapshot.OldValues = BaseEntityEventProvider.NormalizeValues(
                snapshot.OldValues ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

            return snapshot;
        }

        /// <summary>
        /// Приводит transport-представление одной колонки к рабочему CLR-снимку.
        /// </summary>
        /// <param name="alias">Алиас колонки в словаре.</param>
        /// <param name="snapshot">Исходный снимок колонки.</param>
        /// <returns>Нормализованный снимок колонки.</returns>
        private static EntityEventColumnSnapshot NormalizeColumn(string alias, EntityEventColumnSnapshot? snapshot)
        {
            snapshot ??= new EntityEventColumnSnapshot();
            snapshot.Alias = string.IsNullOrWhiteSpace(snapshot.Alias) ? alias : snapshot.Alias;
            snapshot.Value = BaseEntityEventProvider.NormalizeValue(snapshot.Value);
            snapshot.DisplayValue = BaseEntityEventProvider.NormalizeValue(snapshot.DisplayValue);
            return snapshot;
        }

        #endregion Members
    }
}
