using System.Collections;
using System.Data;
using System.Data.Common;
using Titanic.Db.Interfaces;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Базовый SQL запрос.
    /// </summary>
    public abstract class BaseQuery : IQuery, IEnumerable<QueryExpression>
    {
        private BaseDbEngine? _engine;
        private QueryBuildResult? _lastBuildResult;

        /// <summary>
        /// SQL выражения запроса.
        /// </summary>
        protected readonly List<QueryExpression> Expressions = new();

        /// <summary>
        /// Провайдер БД, через который запрос может выполняться.
        /// </summary>
        public BaseDbProvider? Provider { get; private set; }

        /// <summary>
        /// Движок взаимодействия с БД.
        /// </summary>
        public BaseDbEngine Engine => Provider?.Engine
            ?? _engine
            ?? throw new InvalidOperationException("Для построения SQL нужно установить провайдер или движок БД.");

        /// <summary>
        /// Параметры последнего построенного SQL запроса.
        /// </summary>
        public IReadOnlyList<QueryParameter> Parameters => _lastBuildResult?.Parameters ?? Array.Empty<QueryParameter>();

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="engine"> Движок SQL-диалекта. </param>
        protected BaseQuery(BaseDbProvider? provider = null, BaseDbEngine? engine = null)
        {
            Provider = provider;
            _engine = engine;
        }

        /// <summary>
        /// Установить провайдер БД.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <returns>Текущий запрос для дальнейшей настройки цепочкой вызовов.</returns>
        public BaseQuery UseProvider(BaseDbProvider provider)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            return this;
        }

        /// <summary>
        /// Установить движок SQL-диалекта без провайдера выполнения.
        /// </summary>
        /// <param name="engine"> Движок SQL-диалекта. </param>
        /// <returns>Текущий запрос для дальнейшей настройки цепочкой вызовов.</returns>
        public BaseQuery UseEngine(BaseDbEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            return this;
        }

        /// <summary>
        /// Сформировать SQL и параметры.
        /// </summary>
        /// <returns>Результат построения запроса с SQL-текстом и списком параметров.</returns>
        public QueryBuildResult Build()
        {
            var context = new QueryBuildContext(Engine);
            var sql = BuildSql(context);
            _lastBuildResult = new QueryBuildResult(sql, context.Parameters.ToList());
            return _lastBuildResult;
        }

        /// <summary>
        /// Получить SQL текст.
        /// </summary>
        /// <returns>SQL-текст, построенный для текущего состояния запроса.</returns>
        public string ToSql()
        {
            return Build().Sql;
        }

        /// <summary>
        /// Выполнить запрос без чтения результата.
        /// </summary>
        /// <returns>Количество строк, затронутых запросом.</returns>
        public int Execute()
        {
            return GetProvider().Execute(this);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк.
        /// </summary>
        /// <param name="handleRow"> Обработчик строки. </param>
        public void Execute(Action<DbDataReader> handleRow)
        {
            GetProvider().Execute(this, handleRow);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк через DbDataReader.
        /// </summary>
        /// <param name="handleRow"> Обработчик строки. </param>
        public void ExecuteReader(Action<DbDataReader> handleRow)
        {
            GetProvider().Execute(this, handleRow);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк через IDataReader.
        /// </summary>
        /// <param name="handleRow"> Обработчик строки. </param>
        public void ExecuteReader(Action<IDataReader> handleRow)
        {
            GetProvider().ExecuteReader(this, handleRow);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <returns>Список результатов, полученных преобразованием каждой строки.</returns>
        public List<T> ExecuteReader<T>(Func<DbDataReader, T> mapRow)
        {
            return GetProvider().ExecuteReader(this, mapRow);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через IDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <returns>Список результатов, полученных преобразованием каждой строки.</returns>
        public List<T> ExecuteReader<T>(Func<IDataReader, T> mapRow)
        {
            return GetProvider().ExecuteReader(this, mapRow);
        }

        /// <summary>
        /// Выполнить запрос и преобразовать строки результата.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="mapRow"> Преобразователь строки. </param>
        /// <returns>Список результатов, полученных преобразованием каждой строки.</returns>
        public List<T> Query<T>(System.Func<DbDataReader, T> mapRow)
        {
            return GetProvider().Query(this, mapRow);
        }

        /// <summary>
        /// Выполнить запрос с получением одного значения.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <returns>Первое значение результата, приведённое к указанному типу, либо значение по умолчанию.</returns>
        public T? ExecuteScalar<T>()
        {
            return GetProvider().ExecuteScalar<T>(this);
        }

        /// <summary>
        /// Построить SQL запрос в существующем контексте.
        /// </summary>
        /// <param name="context"> Контекст построения. </param>
        protected internal abstract string BuildSql(QueryBuildContext context);

        private BaseDbProvider GetProvider()
        {
            return Provider ?? throw new InvalidOperationException("Для выполнения запроса нужно установить провайдер БД.");
        }

        /// <summary>
        /// Перечислить SQL-выражения, из которых состоит текущий запрос.
        /// </summary>
        /// <returns>Перечислитель выражений запроса.</returns>
        public IEnumerator<QueryExpression> GetEnumerator()
        {
            foreach (var expression in Expressions)
            {
                yield return expression;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
