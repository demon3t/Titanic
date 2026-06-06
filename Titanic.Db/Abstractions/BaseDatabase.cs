using System.Data;
using System.Data.Common;
using Titanic.Db.Builders;
using Titanic.Db.Enums;
using Titanic.Db.Interfaces;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// База для пользовательских классов-обёрток подключения.
    /// Каждая обёртка должна наследоваться от <see cref="BaseDatabase"/>
    /// и нести атрибут <see cref="Attributes.DatabaseConnectionAttribute"/>
    /// или явно указываться в конфиге.
    /// Также предоставляет fluent-методы для построения запросов
    /// (<see cref="Select()"/>, <see cref="Insert(string)"/>, и т.д.) сокращая вызовы.
    /// </summary>
    public abstract class BaseDatabase
    {
        /// <summary>
        /// Имя подключения, по которому обёртка регистрируется в <see cref="DbManager"/>.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Провайдер БД. Доступен только наследникам.
        /// </summary>
        protected BaseDbProvider Provider { get; private set; } = null!;

        /// <summary>
        /// Движок SQL-диалекта. Доступен только наследникам.
        /// </summary>
        protected BaseDbEngine Engine { get; private set; } = null!;

        /// <summary>
        /// Строка подключения. Доступен только наследникам.
        /// </summary>
        protected string ConnectionString => Provider?.ConnectionString ?? string.Empty;

        /// <summary>
        /// Инициализация обёртки с конкретным провайдером.
        /// Вызывается инфраструктурой <see cref="DbManager"/>.
        /// </summary>
        /// <param name="name">Имя подключения.</param>
        /// <param name="provider">Провайдер БД.</param>
        public virtual void Initialize(string name, BaseDbProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Provider = provider;
            Engine = provider.Engine;
        }

        #region Query builders (shortcuts)

        /// <summary>
        /// Создать SELECT билдер для провайдера обёртки.
        /// </summary>
        public Select Select() => Provider.Select();

        /// <summary>
        /// Создать UPDATE билдер для указанной таблицы.
        /// </summary>
        public Update Update(string tableName) => Provider.Update(tableName);

        /// <summary>
        /// Создать DELETE билдер для указанной таблицы.
        /// </summary>
        public Delete Delete(string tableName) => Provider.Delete(tableName);

        /// <summary>
        /// Создать INSERT билдер для указанной таблицы.
        /// </summary>
        public InsertSelect Insert(string tableName) => Provider.Insert(tableName);

        #endregion Query builders (shortcuts)

        #region Execute helpers

        /// <summary>
        /// Выполнить запрос с чтением строк через DbDataReader.
        /// </summary>
        public void ExecuteReader(IQuery query, Action<DbDataReader> handleRow)
            => Provider.Execute(query, handleRow);

        /// <summary>
        /// Выполнить запрос с чтением строк через IDataReader.
        /// </summary>
        public void ExecuteReader(IQuery query, Action<IDataReader> handleRow)
            => Provider.ExecuteReader(query, handleRow);

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        public List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
            => Provider.ExecuteReader(query, mapRow);

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через IDataReader.
        /// </summary>
        public List<T> ExecuteReader<T>(IQuery query, Func<IDataReader, T> mapRow)
            => Provider.ExecuteReader(query, mapRow);

        /// <summary>
        /// Выполнить SQL с чтением строк через DbDataReader.
        /// </summary>
        public void ExecuteReader(string sql, Action<DbDataReader> handleRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.Execute(sql, parameters, handleRow);

        /// <summary>
        /// Выполнить SQL с чтением строк через IDataReader.
        /// </summary>
        public void ExecuteReader(string sql, Action<IDataReader> handleRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.ExecuteReader(sql, parameters, handleRow);

        /// <summary>
        /// Выполнить SQL с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        public List<T> ExecuteReader<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.ExecuteReader(sql, parameters, mapRow);

        /// <summary>
        /// Выполнить SQL с чтением строк и вернуть готовый список результатов через IDataReader.
        /// </summary>
        public List<T> ExecuteReader<T>(string sql, Func<IDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.ExecuteReader(sql, parameters, mapRow);

        #endregion Execute helpers

        #region Connection check

        /// <summary>
        /// Проверить доступность БД через лёгкий <c>SELECT TRUE</c>-запрос.
        /// При ошибке возвращает <c>false</c> и прокидывает исключение через параметр.
        /// Удобно для smoke-test'ов и условного скипа интеграционных тестов.
        /// </summary>
        /// <param name="exception">Сюда записывается исключение, если подключение не удалось.</param>
        /// <returns><c>true</c>, если БД ответила; иначе <c>false</c>.</returns>
        public bool CheckConnection(ref Exception exception)
        {
            try
            {
                return Provider.ExecuteScalar<bool>("SELECT TRUE");
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        #endregion Connection check
    }
}
