using Titanic.Db.Abstractions;

namespace Titanic.Db.PosgreSql
{
    /// <summary>
    /// Реализация <see cref="Database"/> для PostgreSQL.
    /// Синглтон: один экземпляр на всё приложение.
    /// </summary>
    public sealed class PostgresDatabase : Database
    {
        #region Конструкторы

        /// <summary>
        /// Конструктор по умолчанию (требуется для синглтона).
        /// </summary>
        public PostgresDatabase()
            : base(new PostgresEngine(), new PostgresProvider(string.Empty))
        {
        }

        /// <summary>
        /// Конструктор с строкой подключения.
        /// </summary>
        /// <param name="connectionString"> Строка подключения к PostgreSQL. </param>
        public PostgresDatabase(string connectionString)
            : base(new PostgresEngine(), new PostgresProvider(connectionString))
        {
        }

        #endregion Конструкторы
    }
}