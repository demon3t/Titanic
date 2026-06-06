using Npgsql;
using System.Data.Common;
using Titanic.Db.Abstractions;

namespace Titanic.Db.PosgreSql
{
    /// <summary>
    /// PosgreSql провайдер.
    /// </summary>
    public class PostgresProvider : BaseDbProvider
    {
        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="connectionString"> Строкуа подключения. </param>
        public PostgresProvider(string connectionString)
            : base(connectionString, new PostgresEngine())
        {

        }

        /// <summary>
        /// Конструктор для reflection-фабрики <see cref="Configuration.ProviderReflectionFactory"/>.
        /// Позволяет поднять провайдер по конфигу с указанием конкретного движка SQL-диалекта.
        /// </summary>
        /// <param name="connectionString"> Строка подключения. </param>
        /// <param name="engine"> Движок SQL-диалекта. </param>
        public PostgresProvider(string connectionString, BaseDbEngine engine)
            : base(connectionString, engine)
        {
        }

        /// <summary>
        /// Создать подключение PostgreSQL.
        /// </summary>
        protected override DbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        /// <summary>
        /// Create PostgreSQL SELECT query builder.
        /// </summary>
        public override Select Select() => new(this);

        /// <summary>
        /// Create PostgreSQL SELECT query builder with columns.
        /// </summary>
        public override Select Select(params string[] columns) => new Select(this).Columns(columns);

        /// <summary>
        /// Create PostgreSQL UPDATE query builder.
        /// </summary>
        public override Update Update() => new(this);

        /// <summary>
        /// Create PostgreSQL UPDATE query builder.
        /// </summary>
        public override Update Update(string tableName) => new Update(this).Table(tableName);

        /// <summary>
        /// Create PostgreSQL DELETE query builder.
        /// </summary>
        public override Delete Delete() => new(this);

        /// <summary>
        /// Create PostgreSQL DELETE query builder.
        /// </summary>
        public override Delete Delete(string tableName) => new Delete(this).From(tableName);

        /// <summary>
        /// Create PostgreSQL INSERT query builder.
        /// </summary>
        public override InsertSelect Insert() => new(this);

        /// <summary>
        /// Create PostgreSQL INSERT query builder.
        /// </summary>
        public override InsertSelect Insert(string tableName) => new InsertSelect(this).Into(tableName);

        /// <summary>
        /// Создать PostgreSQL параметр.
        /// </summary>
        /// <param name="parameter"> Универсальное описание параметра. </param>
        protected override DbParameter CreateParameter(QueryParameter parameter)
        {
            return new NpgsqlParameter(parameter.Name, parameter.Value ?? DBNull.Value);
        }
    }
}
