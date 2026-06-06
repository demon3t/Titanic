using System.Data.Common;
using Titanic.Db.Interfaces;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Абстрактная база для типизированной БД.
    /// Хранит SQL engine и провайдер подключения, а также предоставляет короткие методы создания и выполнения запросов.
    /// </summary>
    public abstract class Database
    {
        #region Fields

        private static readonly Dictionary<Type, Database> _instances = new();
        private static readonly object _lock = new();

        #endregion Fields

        #region Properties

        /// <summary>
        /// SQL engine текущего провайдера БД.
        /// </summary>
        protected BaseDbEngine Engine { get; }

        /// <summary>
        /// Провайдер БД.
        /// </summary>
        public BaseDbProvider Provider { get; }

        /// <summary>
        /// Строка подключения текущего провайдера.
        /// </summary>
        public string ConnectionString => Provider.ConnectionString;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Создать экземпляр типизированной БД.
        /// </summary>
        /// <param name="engine"> SQL engine. </param>
        /// <param name="provider"> Провайдер БД. </param>
        protected Database(BaseDbEngine engine, BaseDbProvider provider)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        #endregion Constructors

        #region Singleton

        /// <summary>
        /// Получить singleton-экземпляр типизированной БД.
        /// </summary>
        /// <typeparam name="T"> Тип БД. </typeparam>
        /// <returns> Singleton-экземпляр БД. </returns>
        public static T GetInstance<T>() where T : Database, new()
        {
            var type = typeof(T);

            if (_instances.TryGetValue(type, out var existing))
            {
                return (T)existing;
            }

            lock (_lock)
            {
                if (_instances.TryGetValue(type, out existing))
                {
                    return (T)existing;
                }

                var instance = new T();
                instance.Initialize();
                _instances[type] = instance;
                return instance;
            }
        }

        /// <summary>
        /// Получить singleton-экземпляр типизированной БД по строке подключения.
        /// </summary>
        /// <typeparam name="T"> Тип БД. </typeparam>
        /// <param name="connectionString"> Строка подключения. </param>
        /// <returns> Singleton-экземпляр БД. </returns>
        public static T GetInstance<T>(string connectionString) where T : Database, new()
        {
            var type = typeof(T);

            if (_instances.TryGetValue(type, out var existing))
            {
                return (T)existing;
            }

            lock (_lock)
            {
                if (_instances.TryGetValue(type, out existing))
                {
                    return (T)existing;
                }

                var instance = (T)Activator.CreateInstance(typeof(T), connectionString)!;
                _instances[type] = instance;
                return instance;
            }
        }

        /// <summary>
        /// Инициализировать экземпляр после создания через <see cref="GetInstance{T}()" />.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        #endregion Singleton

        #region Query Builders

        /// <summary>
        /// Создать SELECT builder.
        /// </summary>
        /// <returns> SELECT builder. </returns>
        public Select Select() => Provider.Select();

        /// <summary>
        /// Создать SELECT builder с колонками.
        /// </summary>
        /// <param name="columns"> Имена колонок. </param>
        /// <returns> SELECT builder. </returns>
        public Select Select(params string[] columns) => Provider.Select(columns);

        /// <summary>
        /// Создать UPDATE builder.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <returns> UPDATE builder. </returns>
        public Update Update(string tableName) => Provider.Update(tableName);

        /// <summary>
        /// Создать DELETE builder.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <returns> DELETE builder. </returns>
        public Delete Delete(string tableName) => Provider.Delete(tableName);

        /// <summary>
        /// Создать INSERT builder.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <returns> INSERT builder. </returns>
        public InsertSelect Insert(string tableName) => Provider.Insert(tableName);

        /// <summary>
        /// Создать DDL builder для таблицы.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <returns> DDL builder таблицы. </returns>
        public Table Table(string tableName) => new(Provider, tableName);

        /// <summary>
        /// Создать DDL builder по CLR-модели с атрибутами таблицы.
        /// </summary>
        /// <typeparam name="TModel"> Тип CLR-модели таблицы. </typeparam>
        /// <returns> DDL builder таблицы. </returns>
        public Table Table<TModel>() => new(Provider, Titanic.Db.Table.ResolveTableName<TModel>());

        #endregion Query Builders

        #region Query Execution

        /// <summary>
        /// Выполнить запрос без чтения результата.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        /// <returns> Количество затронутых строк. </returns>
        public int Execute(IQuery query) => Provider.Execute(query);

        /// <summary>
        /// Выполнить SQL без чтения результата.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <returns> Количество затронутых строк. </returns>
        public int Execute(string sql, IEnumerable<QueryParameter>? parameters = null)
            => Provider.Execute(sql, parameters);

        /// <summary>
        /// Выполнить запрос и преобразовать строки результата.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <returns> Список результатов. </returns>
        public List<T> Query<T>(IQuery query, Func<DbDataReader, T> mapRow)
            => Provider.Query(query, mapRow);

        /// <summary>
        /// Выполнить SQL и преобразовать строки результата.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <returns> Список результатов. </returns>
        public List<T> Query<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.ExecuteReader(sql, parameters, mapRow);

        /// <summary>
        /// Выполнить запрос с получением одного значения.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <returns> Значение первой колонки первой строки. </returns>
        public T? ExecuteScalar<T>(IQuery query) => Provider.ExecuteScalar<T>(query);

        /// <summary>
        /// Выполнить SQL с получением одного значения.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <returns> Значение первой колонки первой строки. </returns>
        public T? ExecuteScalar<T>(string sql, IEnumerable<QueryParameter>? parameters = null)
            => Provider.ExecuteScalar<T>(sql, parameters);

        /// <summary>
        /// Выполнить запрос с чтением строк через <see cref="DbDataReader" />.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        public void ExecuteReader(IQuery query, Action<DbDataReader> handleRow)
            => Provider.Execute(query, handleRow);

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <returns> Список результатов. </returns>
        public List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
            => Provider.ExecuteReader(query, mapRow);

        /// <summary>
        /// Выполнить SQL с чтением строк через <see cref="DbDataReader" />.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        /// <param name="parameters"> Параметры. </param>
        public void ExecuteReader(string sql, Action<DbDataReader> handleRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.Execute(sql, parameters, handleRow);

        /// <summary>
        /// Выполнить SQL с чтением строк и вернуть готовый список результатов.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <returns> Список результатов. </returns>
        public List<T> ExecuteReader<T>(string sql, Func<DbDataReader, T> mapRow, IEnumerable<QueryParameter>? parameters = null)
            => Provider.ExecuteReader(sql, parameters, mapRow);

        /// <summary>
        /// Получить значение одной колонки из строки результата запроса.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="columnName"> Имя колонки. </param>
        /// <returns> Значение колонки или значение по умолчанию. </returns>
        public T? Get<T>(IQuery query, string columnName)
        {
            var result = default(T?);
            Provider.Execute(query, reader =>
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (!reader.IsDBNull(ordinal))
                {
                    result = (T)Convert.ChangeType(reader.GetValue(ordinal), typeof(T));
                }
            });
            return result;
        }

        /// <summary>
        /// Получить значение одной колонки из строки результата SQL запроса.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="columnName"> Имя колонки. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <returns> Значение колонки или значение по умолчанию. </returns>
        public T? Get<T>(string sql, string columnName, IEnumerable<QueryParameter>? parameters = null)
        {
            var result = default(T?);
            Provider.Execute(sql, parameters, reader =>
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (!reader.IsDBNull(ordinal))
                {
                    result = (T)Convert.ChangeType(reader.GetValue(ordinal), typeof(T));
                }
            });
            return result;
        }

        #endregion Query Execution
    }
}
