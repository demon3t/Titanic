using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using System.Globalization;

namespace Titanic.Db.PosgreSql
{
    /// <summary>
    /// Движок для Postgres.
    /// </summary>
    public class PostgresEngine : BaseDbEngine
    {
        /// <summary>
        /// Тип БД.
        /// </summary>
        public override DatabaseType DatabaseType => DatabaseType.Postgres;

        /// <summary>
        /// Префикс параметров PostgreSQL.
        /// </summary>
        public override string ParameterPrefix => "@";

        /// <summary>
        /// Экранировать PostgreSQL идентификатор.
        /// </summary>
        public override string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("Identifier is empty", nameof(identifier));
            }

            if (identifier == "*")
            {
                return "*";
            }

            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        public override string GetParameterSqlString<T>(T param)
        {
            return param switch
            {
                int value => value.ToString(CultureInfo.InvariantCulture),
                long value => value.ToString(CultureInfo.InvariantCulture),
                decimal value => value.ToString(CultureInfo.InvariantCulture),
                float value => value.ToString(CultureInfo.InvariantCulture),
                double value => value.ToString(CultureInfo.InvariantCulture),
                string value => $"'{value.Replace("'", "''")}'",
                DateTime value => $"'{value:yyyy-MM-dd HH:mm:ss}'",
                Guid value => $"'{value}'",
                bool value => value ? "true" : "false",
                null => "NULL",
                _ => throw new NotSupportedException($"Тип {typeof(T)} не поддерживается")
            };
        }
    }
}
