namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Контекст построения SQL запроса.
    /// </summary>
    public sealed class QueryBuildContext
    {
        private readonly List<QueryParameter> _parameters = new();

        /// <summary>
        /// Движок SQL-диалекта.
        /// </summary>
        public BaseDbEngine Engine { get; }

        /// <summary>
        /// Параметры запроса.
        /// </summary>
        public IReadOnlyList<QueryParameter> Parameters => _parameters;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="engine"> Движок SQL-диалекта. </param>
        public QueryBuildContext(BaseDbEngine engine)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Добавить параметр и получить placeholder для SQL текста.
        /// </summary>
        /// <param name="value"> Значение параметра. </param>
        public string AddParameter(object? value)
        {
            var parameterName = Engine.BuildParameterName(_parameters.Count);
            _parameters.Add(new QueryParameter(parameterName, value));
            return Engine.GetParameterPlaceholder(parameterName);
        }
    }
}