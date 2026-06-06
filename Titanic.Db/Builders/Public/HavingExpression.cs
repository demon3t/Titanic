using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Fluent-обёртка над агрегатным выражением для условия HAVING.
    /// Позволяет сравнивать результат агрегата через цепочку <c>Is*</c>-методов.
    /// Создаётся через <see cref="Select.Having(QueryExpression)"/> (когда передаётся агрегат).
    /// Не отдаёт внутреннее <c>QueryExpression</c> наружу.
    /// </summary>
    public class HavingExpression
    {
        private readonly Select _select;
        private readonly QueryExpression _aggregate;
        private bool _negated;

        internal HavingExpression(Select select, QueryExpression aggregate)
        {
            ArgumentNullException.ThrowIfNull(select);
            ArgumentNullException.ThrowIfNull(aggregate);

            _select = select;
            _aggregate = aggregate;
        }

        /// <summary>
        /// Включить отрицание для следующего оператора сравнения.
        /// </summary>
        public HavingExpression Not()
        {
            _negated = true;
            return this;
        }

        /// <summary>
        /// Агрегат = value.
        /// </summary>
        public Select IsEqual(object? value) => Build(value, ConditionOperator.Equal);

        /// <summary>
        /// Агрегат > value.
        /// </summary>
        public Select IsGreaterThan(object? value) => Build(value, ConditionOperator.GreaterThan);

        /// <summary>
        /// Агрегат >= value.
        /// </summary>
        public Select IsGreaterOrEqual(object? value) => Build(value, ConditionOperator.GreaterThanOrEqual);

        /// <summary>
        /// Агрегат < value.
        /// </summary>
        public Select IsLess(object? value) => Build(value, ConditionOperator.LessThan);

        /// <summary>
        /// Агрегат <= value.
        /// </summary>
        public Select IsLessOrEqual(object? value) => Build(value, ConditionOperator.LessThanOrEqual);

        private Select Build(object? value, ConditionOperator op)
        {
            QueryExpression valueExpr = value is QueryExpression qe
                ? qe
                : QueryExpression.Param(value);

            var expr = QueryExpression.Binary(_aggregate, op, valueExpr);
            if (_negated)
            {
                expr = QueryExpression.Not(expr);
            }

            _select.HavingCondition(expr);
            return _select;
        }
    }
}
