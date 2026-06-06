namespace Titanic.Db.Configuration
{
    /// <summary>
    /// Конфигурация провайдеров БД.
    /// </summary>
    public sealed class DbConfig
    {
        /// <summary>
        /// Имя провайдера по умолчанию. Если пусто — используется первый провайдер из списка.
        /// </summary>
        public string? DefaultProviderName { get; set; }

        /// <summary>
        /// Настроенные провайдеры БД.
        /// </summary>
        public List<DbProviderConfig> Providers { get; set; } = new();
    }
}