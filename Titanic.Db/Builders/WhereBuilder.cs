using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер условий WHERE, HAVING и ON.
    /// </summary>
    /// <typeparam name="TParent">Тип родительского билдера.</typeparam>
    public class WhereBuilder<TParent>
    {
        #region Поля

        private readonly TParent _parent;
        private readonly Func<QueryExpression, TParent> _apply;
        private readonly Stack<(string Operator, string ParentOperator, List<QueryExpression> Items)> _groups = new();
        private QueryExpression? _current;
        private string _nextOperator = "AND";

        #endregion Поля

        #region Конструкторы

        public WhereBuilder(TParent parent, Func<QueryExpression, TParent> apply)
        {
            _parent = parent;
            _apply = apply;
        }

        #endregion Конструкторы

        #region Операторы

        public WhereBuilder<TParent> Equal(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.Equal, value));

        public WhereBuilder<TParent> Equal(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.Equal, value));

        public WhereBuilder<TParent> NotEqual(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.NotEqual, value));

        public WhereBuilder<TParent> NotEqual(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.NotEqual, value));

        public WhereBuilder<TParent> GreaterThan(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.GreaterThan, value));

        public WhereBuilder<TParent> GreaterThan(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.GreaterThan, value));

        public WhereBuilder<TParent> GreaterThanOrEqual(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.GreaterThanOrEqual, value));

        public WhereBuilder<TParent> GreaterThanOrEqual(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.GreaterThanOrEqual, value));

        public WhereBuilder<TParent> LessThan(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.LessThan, value));

        public WhereBuilder<TParent> LessThan(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.LessThan, value));

        public WhereBuilder<TParent> LessThanOrEqual(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.LessThanOrEqual, value));

        public WhereBuilder<TParent> LessThanOrEqual(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.LessThanOrEqual, value));

        public WhereBuilder<TParent> Between(string columnName, QueryExpression from, QueryExpression to)
            => Add(QueryExpression.And(
                Compare(columnName, ConditionOperator.GreaterThanOrEqual, from),
                Compare(columnName, ConditionOperator.LessThanOrEqual, to)));

        public WhereBuilder<TParent> NotBetween(string columnName, QueryExpression from, QueryExpression to)
            => Add(QueryExpression.Or(
                Compare(columnName, ConditionOperator.LessThan, from),
                Compare(columnName, ConditionOperator.GreaterThan, to)));

        public WhereBuilder<TParent> In(string columnName, params QueryExpression[] values)
            => Add(Compare(columnName, ConditionOperator.In, QueryExpression.List(values)));

        public WhereBuilder<TParent> In(string columnName, BaseQuery subQuery)
            => Add(QueryExpression.In(columnName, subQuery));

        public WhereBuilder<TParent> In(string alias, string columnName, BaseQuery subQuery)
            => Add(Compare(alias, columnName, ConditionOperator.In, QueryExpression.SubQuery(subQuery)));

        public WhereBuilder<TParent> NotIn(string columnName, params QueryExpression[] values)
            => Add(Compare(columnName, ConditionOperator.NotIn, QueryExpression.List(values)));

        public WhereBuilder<TParent> NotIn(string columnName, BaseQuery subQuery)
            => Add(QueryExpression.NotIn(columnName, subQuery));

        public WhereBuilder<TParent> Like(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.Like, value));

        public WhereBuilder<TParent> Like(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.Like, value));

        public WhereBuilder<TParent> NotLike(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.NotLike, value));

        public WhereBuilder<TParent> NotLike(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.NotLike, value));

        public WhereBuilder<TParent> IsNull(string columnName) => Add(QueryExpression.IsNull(columnName));

        public WhereBuilder<TParent> IsNull(string alias, string columnName) => Add(QueryExpression.IsNull(alias, columnName));

        public WhereBuilder<TParent> Exists(BaseQuery subQuery) => Add(QueryExpression.Exists(subQuery));

        public WhereBuilder<TParent> NotExists(BaseQuery subQuery) => Add(QueryExpression.Not(QueryExpression.Exists(subQuery)));

        #endregion Операторы

        #region Связки и скобки

        public WhereBuilder<TParent> And()
        {
            _nextOperator = "AND";
            return this;
        }

        public WhereBuilder<TParent> Or()
        {
            _nextOperator = "OR";
            return this;
        }

        public WhereBuilder<TParent> AndOpen()
        {
            _groups.Push(("AND", _nextOperator, new List<QueryExpression>()));
            _nextOperator = "AND";
            return this;
        }

        public WhereBuilder<TParent> OrOpen()
        {
            _groups.Push(("OR", _nextOperator, new List<QueryExpression>()));
            _nextOperator = "AND";
            return this;
        }

        public WhereBuilder<TParent> Close()
        {
            if (_groups.Count == 0)
            {
                throw new InvalidOperationException("No opened condition group");
            }

            var group = _groups.Pop();
            var expression = group.Operator == "OR"
                ? QueryExpression.Or(group.Items.ToArray())
                : QueryExpression.And(group.Items.ToArray());

            Add(expression, group.ParentOperator);
            return this;
        }

        #endregion Связки и скобки

        #region Методы

        /// <summary>
        /// Завершить построение условий и вернуть родительский билдер.
        /// </summary>
        public TParent End()
        {
            while (_groups.Count > 0)
            {
                Close();
            }

            return _current == null ? _parent : _apply(_current);
        }

        /// <summary>
        /// Применить накопленные условия и вернуть родительский билдер.
        /// </summary>
        internal TParent Apply()
        {
            while (_groups.Count > 0)
            {
                Close();
            }

            return _current == null ? _parent : _apply(_current);
        }

        private static QueryExpression Compare(string columnName, ConditionOperator op, QueryExpression value)
            => QueryExpression.Binary(QueryExpression.Column(columnName), op, value);

        private static QueryExpression Compare(string alias, string columnName, ConditionOperator op, QueryExpression value)
            => QueryExpression.Binary(QueryExpression.Column(alias, columnName), op, value);

        private WhereBuilder<TParent> Add(QueryExpression expression, string? connector = null)
        {
            if (_groups.Count > 0)
            {
                _groups.Peek().Items.Add(expression);
            }
            else
            {
                var op = connector ?? _nextOperator;
                _current = _current == null
                    ? expression
                    : op == "OR" ? QueryExpression.Or(_current, expression) : QueryExpression.And(_current, expression);
            }

            _nextOperator = "AND";
            return this;
        }

        #endregion Методы
    }
}
