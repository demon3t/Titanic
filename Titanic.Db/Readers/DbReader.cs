using System.Data.Common;
using Titanic.Db.Interfaces;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Универсальный ридер из БД.
    /// Делегирует работу провайдеру, полученному из статического <see cref="DbManager"/> по имени или по умолчанию.
    /// </summary>
    public sealed class DbReader : IDbReader
    {
        /// <summary>
        /// Создать ридер, использующий стандартный провайдер из <see cref="DbManager"/>.
        /// </summary>
        public DbReader()
        {
        }

        /// <summary>
        /// Создать ридер с принудительной регистрацией провайдера в <see cref="DbManager"/>.
        /// </summary>
        /// <param name="provider"> Провайдер для регистрации. </param>
        /// <param name="providerName"> Имя провайдера (по умолчанию "default"). </param>
        public DbReader(BaseDbProvider provider, string providerName = "default")
        {
            ArgumentNullException.ThrowIfNull(provider);

            DbManager.RegisterProvider(providerName, provider);
        }

        #region Query

        /// <inheritdoc />
        public List<T> Query<T>(IQuery query, Func<DbDataReader, T> mapRow, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).Query(query, mapRow);
        }

        /// <inheritdoc />
        public List<T> Query<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            var provider = DbManager.GetProvider(providerName);
            var rows = new List<T>();
            provider.Execute(sql, parameters, reader => rows.Add(mapRow(reader)));
            return rows;
        }

        #endregion Query

        #region ExecuteScalar

        /// <inheritdoc />
        public T? ExecuteScalar<T>(IQuery query, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).ExecuteScalar<T>(query);
        }

        /// <inheritdoc />
        public T? ExecuteScalar<T>(string sql, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).ExecuteScalar<T>(sql, parameters);
        }

        #endregion ExecuteScalar

        #region Execute

        /// <inheritdoc />
        public int Execute(IQuery query, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).Execute(query);
        }

        /// <inheritdoc />
        public int Execute(string sql, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).Execute(sql, parameters);
        }

        #endregion Execute

        #region ExecuteReader

        /// <inheritdoc />
        public void ExecuteReader(IQuery query, Action<DbDataReader> handleRow, string? providerName = null)
        {
            DbManager.GetProvider(providerName).Execute(query, handleRow);
        }

        /// <inheritdoc />
        public List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).ExecuteReader(query, mapRow);
        }

        /// <inheritdoc />
        public void ExecuteReader(string sql, Action<DbDataReader> handleRow, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            DbManager.GetProvider(providerName).Execute(sql, parameters, handleRow);
        }

        /// <inheritdoc />
        public List<T> ExecuteReader<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            return DbManager.GetProvider(providerName).ExecuteReader(sql, parameters, mapRow);
        }

        #endregion ExecuteReader

        #region GetColumnValue

        /// <inheritdoc />
        public T? GetColumnValue<T>(IQuery query, string columnName, string? providerName = null)
        {
            var provider = DbManager.GetProvider(providerName);
            var result = default(T?);

            provider.Execute(query, reader =>
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (!reader.IsDBNull(ordinal))
                {
                    result = DbValueConverter.ConvertTo<T>(reader.GetValue(ordinal));
                }
            });

            return result;
        }

        /// <inheritdoc />
        public T? GetColumnValue<T>(string sql, string columnName, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            var provider = DbManager.GetProvider(providerName);
            var result = default(T?);

            provider.Execute(sql, parameters, reader =>
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (!reader.IsDBNull(ordinal))
                {
                    result = DbValueConverter.ConvertTo<T>(reader.GetValue(ordinal));
                }
            });

            return result;
        }

        #endregion GetColumnValue

        #region Get

        /// <inheritdoc />
        public T? Get<T>(IQuery query, string columnName, string? providerName = null)
        {
            return GetColumnValue<T>(query, columnName, providerName);
        }

        /// <inheritdoc />
        public T? Get<T>(string sql, string columnName, IEnumerable<QueryParameter>? parameters = null, string? providerName = null)
        {
            return GetColumnValue<T>(sql, columnName, parameters, providerName);
        }

        #endregion Get
    }
}
