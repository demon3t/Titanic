namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель значения колонки Entity API.
    /// </summary>
    public sealed class EntityApiColumnValueResponse
    {
        /// <summary>
        /// Сырое значение колонки.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Отображаемое значение колонки.
        /// </summary>
        public object? DisplayValue { get; set; }
    }
}
