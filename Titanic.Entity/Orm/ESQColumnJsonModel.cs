using System.Text.Json.Serialization;

namespace Titanic.Entity.Orm
{
    public sealed class ESQColumnJsonModel
    {
        public string Path { get; set; } = string.Empty;

        public string? Alias { get; set; }

        /// <summary>
        /// Тип агрегатной функции для колонки.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityAggregationType AggregationType { get; set; }
    }
}
