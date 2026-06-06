namespace Titanic.Db.Configuration
{
    /// <summary>
    /// Конфигурация пула подключений для одного провайдера.
    /// </summary>
    public sealed class ConnectionPoolConfig
    {
        /// <summary>
        /// Максимальное количество кешированных подключений на провайдер.
        /// По умолчанию 8.
        /// </summary>
        public int MaxPoolSize { get; set; } = 8;

        /// <summary>
        /// Минимальное количество подключений, которые держат открытыми.
        /// По умолчанию 0.
        /// </summary>
        public int MinPoolSize { get; set; } = 0;

        /// <summary>
        /// Время жизни подключения в секундах. 0 — без ограничения.
        /// </summary>
        public int ConnectionLifetimeSeconds { get; set; } = 0;

        /// <summary>
        /// Время жизни неактивного подключения в секундах. 0 — без ограничения.
        /// </summary>
        public int ConnectionIdleTimeoutSeconds { get; set; } = 0;
    }
}