using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер условий HAVING.
    /// </summary>
    /// <typeparam name="TParent">Тип родительского билдера.</typeparam>
    internal class HavingBuilder<TParent>
    {
        #region Поля

        private readonly TParent _parent;
        private readonly Func<QueryExpression, TParent> _apply;
        private QueryExpression? _current;
        private string _nextOperator = "AND";

        #endregion Поля

        #region Конструкторы

        public HavingBuilder(TParent parent, Func<QueryExpression, TParent> apply)
        {
            _parent = parent;
            _apply = apply;
        }

        #endregion Конструкторы

        #region Операторы

        public HavingBuilder<TParent> Equal(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.Equal, value));

        public HavingBuilder<TParent> Equal(string alias, string columnName, QueryExpression value)
            => Add(Compare(alias, columnName, ConditionOperator.Equal, value));

        public HavingBuilder<TParent> NotEqual(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.NotEqual, value));

        public HavingBuilder<TParent> GreaterThan(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.GreaterThan, value));

        public HavingBuilder<TParent> GreaterThan(string alias, string columnName, QueryExpression value)
            => Add(Compare(alias, columnName, ConditionOperator.GreaterThan, value));

        public HavingBuilder<TParent> GreaterThanOrEqual(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.GreaterThanOrEqual, value));

        public HavingBuilder<TParent> LessThan(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.LessThan, value));

        public HavingBuilder<TParent> LessThanOrEqual(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.LessThanOrEqual, value));

        public HavingBuilder<TParent> Like(string columnName, QueryExpression value)
            => Add(Compare(columnName, ConditionOperator.Like, value));

        public HavingBuilder<TParent> IsNull(string columnName)
            => Add(QueryExpression.IsNull(columnName));

        public HavingBuilder<TParent> Exists(BaseQuery subQuery)
            => Add(QueryExpression.Exists(subQuery));

        public HavingBuilder<TParent> NotExists(BaseQuery subQuery)
            => Add(QueryExpression.Not(QueryExpression.Exists(subQuery)));

        #endregion Операторы

        #region Связки

        public HavingBuilder<TParent> And()
        {
            _nextOperator = "AND";
            return this;
        }

        public HavingBuilder<TParent> Or()
        {
            _nextOperator = "OR";
            return this;
        }

        #endregion Связки

        #region Методы

        /// <summary>
        /// Завершить построение HAVING и вернуть родительский билдер.
        /// </summary>
        public TParent End()
        {
            return _current == null ? _parent : _apply(_current);
        }

        private static QueryExpression Compare(string columnName, ConditionOperator op, QueryExpression value)
            => QueryExpression.Binary(QueryExpression.Column(columnName), op, value);

        private static QueryExpression Compare(string alias, string columnName, ConditionOperator op, QueryExpression value)
            => QueryExpression.Binary(QueryExpression.Column(alias, columnName), op, value);

        private HavingBuilder<TParent> Add(QueryExpression expression)
        {
            var op = _nextOperator;
            _current = _current == null
                ? expression
                : op == "OR" ? QueryExpression.Or(_current, expression) : QueryExpression.And(_current, expression);

            _nextOperator = "AND";
            return this;
        }

        #endregion Методы
    }
}
