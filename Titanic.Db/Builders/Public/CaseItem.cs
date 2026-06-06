using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Флюент-билдер для CASE выражений.
    /// Пример:
    /// Func.Case()
    ///     .When(Column.Name("status")).IsEqual(Column.Parameter(1)).Then(Column.Const("one"))
    ///     .Else(Column.Const("other"));
    /// </summary>
    public class CaseItem
    {
        private readonly List<(QueryExpression When, QueryExpression Then)> _branches = new();

        /// <summary>
        /// Начать WHEN-ветку с левого выражения сравнения.
        /// </summary>
        public CaseWhenItem When(QueryExpression left)
        {
            return new CaseWhenItem(this, left);
        }

        internal CaseItem AddBranch(QueryExpression whenExpression, QueryExpression thenExpression)
        {
            _branches.Add((whenExpression, thenExpression));
            return this;
        }

        /// <summary>
        /// Завершить CASE выражение веткой ELSE.
        /// </summary>
        public QueryExpression Else(QueryExpression elseExpression)
        {
            if (_branches.Count == 0)
                throw new InvalidOperationException("CASE must contain at least one WHEN ... THEN branch");

            return QueryExpression.Case(_branches, elseExpression);
        }
    }

    /// <summary>
    /// Этап выбора оператора сравнения для WHEN.
    /// </summary>
    public class CaseWhenItem
    {
        private readonly CaseItem _caseItem;
        private readonly QueryExpression _left;

        internal CaseWhenItem(CaseItem caseItem, QueryExpression left)
        {
            _caseItem = caseItem;
            _left = left;
        }

        public CaseThenItem IsEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.Equal, right);

        public CaseThenItem IsEqual(object? value)
            => IsEqual(QueryExpression.Param(value));

        public CaseThenItem IsNotEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.NotEqual, right);

        public CaseThenItem IsNotEqual(object? value)
            => IsNotEqual(QueryExpression.Param(value));

        public CaseThenItem IsGreaterThan(QueryExpression right)
            => Build(Enums.ConditionOperator.GreaterThan, right);

        public CaseThenItem IsGreaterThan(object? value)
            => IsGreaterThan(QueryExpression.Param(value));

        public CaseThenItem IsGreaterOrEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.GreaterThanOrEqual, right);

        public CaseThenItem IsGreaterOrEqual(object? value)
            => IsGreaterOrEqual(QueryExpression.Param(value));

        public CaseThenItem IsLess(QueryExpression right)
            => Build(Enums.ConditionOperator.LessThan, right);

        public CaseThenItem IsLess(object? value)
            => IsLess(QueryExpression.Param(value));

        public CaseThenItem IsLessOrEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.LessThanOrEqual, right);

        public CaseThenItem IsLessOrEqual(object? value)
            => IsLessOrEqual(QueryExpression.Param(value));

        public CaseThenItem IsLike(QueryExpression right)
            => Build(Enums.ConditionOperator.Like, right);

        public CaseThenItem IsLike(object? value)
            => IsLike(QueryExpression.Param(value));

        private CaseThenItem Build(Enums.ConditionOperator op, QueryExpression right)
        {
            var whenExpression = QueryExpression.Binary(_left, op, right);
            return new CaseThenItem(_caseItem, whenExpression);
        }
    }

    /// <summary>
    /// Этап указания THEN выражения.
    /// </summary>
    public class CaseThenItem
    {
        private readonly CaseItem _caseItem;
        private readonly QueryExpression _whenExpression;

        internal CaseThenItem(CaseItem caseItem, QueryExpression whenExpression)
        {
            _caseItem = caseItem;
            _whenExpression = whenExpression;
        }

        public CaseItem Then(QueryExpression thenExpression)
        {
            return _caseItem.AddBranch(_whenExpression, thenExpression);
        }
    }
}