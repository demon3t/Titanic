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

        /// <summary>
        /// Создать билдер условий и callback для применения готового выражения.
        /// </summary>
        public WhereBuilder(TParent parent, Func<QueryExpression, TParent> apply)
        {
            _parent = parent;
            _apply = apply;
        }

        #endregion Конструкторы

        #region Операторы

        /// <summary>Добавить условие равенства для колонки.</summary>
        public WhereBuilder<TParent> Equal(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.Equal, value));

        /// <summary>Добавить условие равенства для колонки с алиасом.</summary>
        public WhereBuilder<TParent> Equal(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.Equal, value));

        /// <summary>Добавить условие неравенства для колонки.</summary>
        public WhereBuilder<TParent> NotEqual(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.NotEqual, value));

        /// <summary>Добавить условие неравенства для колонки с алиасом.</summary>
        public WhereBuilder<TParent> NotEqual(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.NotEqual, value));

        /// <summary>Добавить условие "больше" для колонки.</summary>
        public WhereBuilder<TParent> GreaterThan(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.GreaterThan, value));

        /// <summary>Добавить условие "больше" для колонки с алиасом.</summary>
        public WhereBuilder<TParent> GreaterThan(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.GreaterThan, value));

        /// <summary>Добавить условие "больше или равно" для колонки.</summary>
        public WhereBuilder<TParent> GreaterThanOrEqual(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.GreaterThanOrEqual, value));

        /// <summary>Добавить условие "больше или равно" для колонки с алиасом.</summary>
        public WhereBuilder<TParent> GreaterThanOrEqual(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.GreaterThanOrEqual, value));

        /// <summary>Добавить условие "меньше" для колонки.</summary>
        public WhereBuilder<TParent> LessThan(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.LessThan, value));

        /// <summary>Добавить условие "меньше" для колонки с алиасом.</summary>
        public WhereBuilder<TParent> LessThan(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.LessThan, value));

        /// <summary>Добавить условие "меньше или равно" для колонки.</summary>
        public WhereBuilder<TParent> LessThanOrEqual(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.LessThanOrEqual, value));

        /// <summary>Добавить условие "меньше или равно" для колонки с алиасом.</summary>
        public WhereBuilder<TParent> LessThanOrEqual(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.LessThanOrEqual, value));

        /// <summary>Добавить условие попадания значения колонки в диапазон.</summary>
        public WhereBuilder<TParent> Between(string columnName, QueryExpression from, QueryExpression to)
            => Add(QueryExpression.And(
                Compare(columnName, ConditionOperator.GreaterThanOrEqual, from),
                Compare(columnName, ConditionOperator.LessThanOrEqual, to)));

        /// <summary>Добавить условие выхода значения колонки за пределы диапазона.</summary>
        public WhereBuilder<TParent> NotBetween(string columnName, QueryExpression from, QueryExpression to)
            => Add(QueryExpression.Or(
                Compare(columnName, ConditionOperator.LessThan, from),
                Compare(columnName, ConditionOperator.GreaterThan, to)));

        /// <summary>Добавить условие IN со списком выражений.</summary>
        public WhereBuilder<TParent> In(string columnName, params QueryExpression[] values)
            => Add(Compare(columnName, ConditionOperator.In, QueryExpression.List(values)));

        /// <summary>Добавить условие IN с подзапросом.</summary>
        public WhereBuilder<TParent> In(string columnName, BaseQuery subQuery)
            => Add(QueryExpression.In(columnName, subQuery));

        /// <summary>Добавить условие IN с подзапросом для колонки с алиасом.</summary>
        public WhereBuilder<TParent> In(string alias, string columnName, BaseQuery subQuery)
            => Add(Compare(alias, columnName, ConditionOperator.In, QueryExpression.SubQuery(subQuery)));

        /// <summary>Добавить условие NOT IN со списком выражений.</summary>
        public WhereBuilder<TParent> NotIn(string columnName, params QueryExpression[] values)
            => Add(Compare(columnName, ConditionOperator.NotIn, QueryExpression.List(values)));

        /// <summary>Добавить условие NOT IN с подзапросом.</summary>
        public WhereBuilder<TParent> NotIn(string columnName, BaseQuery subQuery)
            => Add(QueryExpression.NotIn(columnName, subQuery));

        /// <summary>Добавить условие LIKE для колонки.</summary>
        public WhereBuilder<TParent> Like(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.Like, value));

        /// <summary>Добавить условие LIKE для колонки с алиасом.</summary>
        public WhereBuilder<TParent> Like(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.Like, value));

        /// <summary>Добавить условие NOT LIKE для колонки.</summary>
        public WhereBuilder<TParent> NotLike(string columnName, QueryExpression value) => Add(Compare(columnName, ConditionOperator.NotLike, value));

        /// <summary>Добавить условие NOT LIKE для колонки с алиасом.</summary>
        public WhereBuilder<TParent> NotLike(string alias, string columnName, QueryExpression value) => Add(Compare(alias, columnName, ConditionOperator.NotLike, value));

        /// <summary>Добавить условие IS NULL для колонки.</summary>
        public WhereBuilder<TParent> IsNull(string columnName) => Add(QueryExpression.IsNull(columnName));

        /// <summary>Добавить условие IS NULL для колонки с алиасом.</summary>
        public WhereBuilder<TParent> IsNull(string alias, string columnName) => Add(QueryExpression.IsNull(alias, columnName));

        /// <summary>Добавить условие EXISTS с подзапросом.</summary>
        public WhereBuilder<TParent> Exists(BaseQuery subQuery) => Add(QueryExpression.Exists(subQuery));

        /// <summary>Добавить условие NOT EXISTS с подзапросом.</summary>
        public WhereBuilder<TParent> NotExists(BaseQuery subQuery) => Add(QueryExpression.Not(QueryExpression.Exists(subQuery)));

        #endregion Операторы

        #region Связки и скобки

        /// <summary>Задать AND для следующего условия.</summary>
        public WhereBuilder<TParent> And()
        {
            _nextOperator = "AND";
            return this;
        }

        /// <summary>Задать OR для следующего условия.</summary>
        public WhereBuilder<TParent> Or()
        {
            _nextOperator = "OR";
            return this;
        }

        /// <summary>Открыть группу условий, соединяемую с текущим выражением через AND.</summary>
        public WhereBuilder<TParent> AndOpen()
        {
            _groups.Push(("AND", _nextOperator, new List<QueryExpression>()));
            _nextOperator = "AND";
            return this;
        }

        /// <summary>Открыть группу условий, соединяемую с текущим выражением через OR.</summary>
        public WhereBuilder<TParent> OrOpen()
        {
            _groups.Push(("OR", _nextOperator, new List<QueryExpression>()));
            _nextOperator = "AND";
            return this;
        }

        /// <summary>Закрыть текущую группу условий и добавить ее к выражению.</summary>
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
