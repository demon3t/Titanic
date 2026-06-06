using System.Text.Json.Serialization;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// JSON-модель сортировки ESQ.
    /// </summary>
    public sealed class ESQOrderJsonModel
    {
        /// <summary>
        /// ORM-путь колонки сортировки.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Направление сортировки.
        /// <c>0</c> - ASC, <c>1</c> - DESC.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public EntityOrderDirection? Direction { get; set; }

        /// <summary>
        /// Legacy-признак сортировки по убыванию.
        /// Используется только для обратной совместимости со старыми JSON-запросами.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Desc { get; set; }

        /// <summary>
        /// Проверить, нужно ли строить сортировку по убыванию.
        /// </summary>
        internal bool IsDescending()
        {
            return Direction switch
            {
                EntityOrderDirection.Descending => true,
                EntityOrderDirection.Ascending => false,
                _ => Desc ?? false
            };
        }
    }
}
