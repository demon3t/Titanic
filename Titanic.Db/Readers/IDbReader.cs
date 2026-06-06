using System.Data.Common;
using Titanic.Db.Interfaces;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Универсальный ридер из БД.
    /// Прослойка между кодом и СУБД, работающая с любым зарегистрированным провайдером.
    /// Провайдер выбирается по имени (providerName) или используется провайдер по умолчанию.
    /// </summary>
    public interface IDbReader
    {
        #region Query

        /// <summary>
        /// Выполнить запрос и преобразовать строки результата.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        List<T> Query<T>(IQuery query, Func<DbDataReader, T> mapRow, string? providerName = null);

        /// <summary>
        /// Выполнить SQL и преобразовать строки результата.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        List<T> Query<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        #endregion Query

        #region ExecuteScalar

        /// <summary>
        /// Выполнить запрос и получить одно значение.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        T? ExecuteScalar<T>(IQuery query, string? providerName = null);

        /// <summary>
        /// Выполнить SQL и получить одно значение.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        T? ExecuteScalar<T>(string sql, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        #endregion ExecuteScalar

        #region Execute

        /// <summary>
        /// Выполнить запрос без чтения результата.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        /// <returns> Количество затронутых строк. </returns>
        int Execute(IQuery query, string? providerName = null);

        /// <summary>
        /// Выполнить SQL без чтения результата.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        /// <returns> Количество затронутых строк. </returns>
        int Execute(string sql, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        #endregion Execute

        #region ExecuteReader

        /// <summary>
        /// Выполнить запрос с чтением строк через DbDataReader.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        void ExecuteReader(IQuery query, Action<DbDataReader> handleRow, string? providerName = null);

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow, string? providerName = null);

        /// <summary>
        /// Выполнить SQL с чтением строк через DbDataReader.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        void ExecuteReader(string sql, Action<DbDataReader> handleRow, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        /// <summary>
        /// Выполнить SQL с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        List<T> ExecuteReader<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        #endregion ExecuteReader

        #region GetColumnValue

        /// <summary>
        /// Получить значение одной колонки из строки результата запроса по имени колонки.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="columnName"> Имя колонки в результате. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        T? GetColumnValue<T>(IQuery query, string columnName, string? providerName = null);

        /// <summary>
        /// Получить значение одной колонки из строки результата SQL запроса по имени колонки.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="columnName"> Имя колонки в результате. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        T? GetColumnValue<T>(string sql, string columnName, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        #endregion GetColumnValue

        #region Get

        /// <summary>
        /// Получить значение одной колонки из строки результата запроса по имени колонки.
        /// Алиас для <see cref="GetColumnValue{T}"/>.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="columnName"> Имя колонки. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        T? Get<T>(IQuery query, string columnName, string? providerName = null);

        /// <summary>
        /// Получить значение одной колонки из строки результата SQL запроса по имени колонки.
        /// Алиас для <see cref="GetColumnValue{T}"/>.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="columnName"> Имя колонки. </param>
        /// <param name="parameters"> Параметры запроса. </param>
        /// <param name="providerName"> Имя провайдера. Если null — используется провайдер по умолчанию. </param>
        T? Get<T>(string sql, string columnName, IEnumerable<QueryParameter>? parameters = null, string? providerName = null);

        #endregion Get
    }
}
