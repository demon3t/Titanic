using Titanic.Db.Enums;

namespace Titanic.Db.Configuration
{
    /// <summary>
    /// Конфигурация одного провайдера БД.
    /// </summary>
    public sealed class DbProviderConfig
    {
        /// <summary>
        /// Настройки пула подключений.
        /// </summary>
        public ConnectionPoolConfig? Pool { get; set; }

        /// <summary>
        /// Имя провайдера, используемое DbManager.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Строка подключения.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Опциональный тип БД для встроенной фабрики (например, <see cref="Enums.DatabaseType.Postgres"/>).
        /// Используется, если не указаны <see cref="ProviderTypeConfig"/>.
        /// </summary>
        public DatabaseType? DatabaseType { get; set; }

        /// <summary>
        /// Опциональное описание типов провайдера/движка/БД для создания через reflection.
        /// Приоритет выше, чем <see cref="DatabaseType"/>.
        /// </summary>
        public ProviderTypeConfig? Types { get; set; }
    }
}