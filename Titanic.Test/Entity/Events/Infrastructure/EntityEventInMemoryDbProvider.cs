using System.Data.Common;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Interfaces;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// In-memory DB provider для событийных тестов без обращения к реальной БД.
    /// </summary>
    internal sealed class EntityEventInMemoryDbProvider : BaseDbProvider
    {
        #region Constructors

        /// <summary>
        /// Создаёт provider с тестовой строкой подключения.
        /// </summary>
        /// <param name="connectionString">Строка подключения.</param>
        /// <param name="engine">SQL-движок.</param>
        public EntityEventInMemoryDbProvider(string connectionString, BaseDbEngine engine)
            : base(connectionString, engine)
        {
        }

        #endregion Constructors

        #region Members

        /// <summary>
        /// Сбрасывает состояние provider-а перед тестом.
        /// </summary>
        public static void ResetState()
        {
        }

        /// <inheritdoc />
        public override int Execute(IQuery query)
        {
            return 1;
        }

        /// <inheritdoc />
        public override T ExecuteScalar<T>(IQuery query)
        {
            object result = typeof(T) switch
            {
                var type when type == typeof(Guid) => Guid.Parse("33333333-3333-3333-3333-333333333333"),
                var type when type == typeof(int) => 1,
                var type when type == typeof(long) => 1L,
                var type when type == typeof(string) => "in-memory",
                var type when type == typeof(object) => Guid.Parse("33333333-3333-3333-3333-333333333333"),
                _ => Activator.CreateInstance<T>()!
            };

            return (T)result;
        }

        /// <inheritdoc />
        public override List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
        {
            return [];
        }

        /// <inheritdoc />
        protected override DbConnection CreateConnection()
        {
            throw new NotSupportedException("EntityEventInMemoryDbProvider does not create database connections.");
        }

        /// <inheritdoc />
        protected override DbParameter CreateParameter(QueryParameter parameter)
        {
            throw new NotSupportedException("EntityEventInMemoryDbProvider does not create database parameters.");
        }

        #endregion Members
    }
}
