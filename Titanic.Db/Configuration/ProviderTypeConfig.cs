namespace Titanic.Db.Configuration
{
    /// <summary>
    /// Описание провайдера через полное имя типа.
    /// Используется, если нужно поднять провайдер через reflection
    /// без явной зависимости на конкретную сборку.
    /// </summary>
    public sealed class ProviderTypeConfig
    {
        /// <summary>
        /// Полное имя типа провайдера БД (assembly-qualified или full name).
        /// Должен наследоваться от <see cref="Titanic.Db.Abstractions.BaseDbProvider"/>.
        /// </summary>
        public string ProviderType { get; set; } = string.Empty;

        /// <summary>
        /// Полное имя типа движка SQL-диалекта (assembly-qualified или full name).
        /// Должен наследоваться от <see cref="Titanic.Db.Abstractions.BaseDbEngine"/>.
        /// </summary>
        public string EngineType { get; set; } = string.Empty;

        /// <summary>
        /// Полное имя типа базы данных (assembly-qualified или full name).
        /// Должен наследоваться от <see cref="Titanic.Db.Abstractions.Database"/>.
        /// </summary>
        public string DatabaseType { get; set; } = string.Empty;
    }
}