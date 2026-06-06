using Titanic.Db.Enums;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Билдер условия для указанной колонки.
    /// Позволяет выбрать оператор и правую часть выражения.
    /// </summary>
    /// <typeparam name="TParent">Тип родительского билдера.</typeparam>
    public sealed class WhereColumnBuilder<TParent>
    {
        private readonly TParent _parent;
        private readonly Func<QueryExpression, TParent> _apply;
        private readonly string? _alias;
        private readonly string _columnName;

        internal WhereColumnBuilder(TParent parent, Func<QueryExpression, TParent> apply, string? alias, string columnName)
        {
            _parent = parent;
            _apply = apply;
            _alias = alias;
            _columnName = columnName;
        }

        /// <summary>Колонка равна выражению.</summary>
        public TParent IsEqual(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.Equal, value));

        /// <summary>Колонка равна другой колонке.</summary>
        public TParent IsEqual(string otherAlias, string otherColumn)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.Equal, QueryExpression.Column(otherAlias, otherColumn)));

        /// <summary>Колонка не равна выражению.</summary>
        public TParent IsNotEqual(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.NotEqual, value));

        /// <summary>Колонка не равна другой колонке.</summary>
        public TParent IsNotEqual(string otherAlias, string otherColumn)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.NotEqual, QueryExpression.Column(otherAlias, otherColumn)));

        /// <summary>Колонка больше выражения.</summary>
        public TParent IsGreaterThan(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.GreaterThan, value));

        /// <summary>Колонка больше другой колонки.</summary>
        public TParent IsGreaterThan(string otherAlias, string otherColumn)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.GreaterThan, QueryExpression.Column(otherAlias, otherColumn)));

        /// <summary>Колонка больше или равна выражению.</summary>
        public TParent IsGreaterThanOrEqual(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.GreaterThanOrEqual, value));

        /// <summary>Колонка больше или равна другой колонке.</summary>
        public TParent IsGreaterThanOrEqual(string otherAlias, string otherColumn)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.GreaterThanOrEqual, QueryExpression.Column(otherAlias, otherColumn)));

        /// <summary>Колонка меньше выражения.</summary>
        public TParent IsLessThan(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.LessThan, value));

        /// <summary>Колонка меньше другой колонки.</summary>
        public TParent IsLessThan(string otherAlias, string otherColumn)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.LessThan, QueryExpression.Column(otherAlias, otherColumn)));

        /// <summary>Колонка меньше или равна выражению.</summary>
        public TParent IsLessThanOrEqual(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.LessThanOrEqual, value));

        /// <summary>Колонка меньше или равна другой колонке.</summary>
        public TParent IsLessThanOrEqual(string otherAlias, string otherColumn)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.LessThanOrEqual, QueryExpression.Column(otherAlias, otherColumn)));

        /// <summary>Колонка соответствует LIKE.</summary>
        public TParent Like(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.Like, value));

        /// <summary>Колонка не соответствует LIKE.</summary>
        public TParent NotLike(QueryExpression value)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.NotLike, value));

        /// <summary>Колонка IS NULL.</summary>
        public TParent IsNull()
            => Add(_alias != null ? QueryExpression.IsNull(_alias, _columnName) : QueryExpression.IsNull(_columnName));

        /// <summary>Колонка IN (подзапрос).</summary>
        public TParent In(BaseQuery subQuery)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.In, QueryExpression.SubQuery(subQuery)));

        /// <summary>Колонка IN (список значений).</summary>
        public TParent In(params QueryExpression[] values)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.In, QueryExpression.List(values)));

        /// <summary>Колонка NOT IN (подзапрос).</summary>
        public TParent NotIn(BaseQuery subQuery)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.NotIn, QueryExpression.SubQuery(subQuery)));

        /// <summary>Колонка NOT IN (список значений).</summary>
        public TParent NotIn(params QueryExpression[] values)
            => Add(QueryExpression.Binary(ColumnExpr(), ConditionOperator.NotIn, QueryExpression.List(values)));

        /// <summary>Колонка находится в диапазоне.</summary>
        public TParent Between(QueryExpression from, QueryExpression to)
            => Add(QueryExpression.And(
                QueryExpression.Binary(ColumnExpr(), ConditionOperator.GreaterThanOrEqual, from),
                QueryExpression.Binary(ColumnExpr(), ConditionOperator.LessThanOrEqual, to)));

        private QueryExpression ColumnExpr()
            => _alias != null ? QueryExpression.Column(_alias, _columnName) : QueryExpression.Column(_columnName);

        private TParent Add(QueryExpression expression) => _apply(expression);
    }
}
