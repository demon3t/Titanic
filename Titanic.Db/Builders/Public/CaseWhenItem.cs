using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Этап выбора оператора сравнения для CASE WHEN.
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

        /// <summary>
        /// Creates a CASE branch that checks equality with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.Equal, right);

        /// <summary>
        /// Creates a CASE branch that checks equality with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsEqual(object? value)
            => IsEqual(QueryExpression.Param(value));

        /// <summary>
        /// Creates a CASE branch that checks inequality with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsNotEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.NotEqual, right);

        /// <summary>
        /// Creates a CASE branch that checks inequality with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsNotEqual(object? value)
            => IsNotEqual(QueryExpression.Param(value));

        /// <summary>
        /// Creates a CASE branch that checks greater-than comparison with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsGreaterThan(QueryExpression right)
            => Build(Enums.ConditionOperator.GreaterThan, right);

        /// <summary>
        /// Creates a CASE branch that checks greater-than comparison with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsGreaterThan(object? value)
            => IsGreaterThan(QueryExpression.Param(value));

        /// <summary>
        /// Creates a CASE branch that checks greater-than-or-equal comparison with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsGreaterOrEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.GreaterThanOrEqual, right);

        /// <summary>
        /// Creates a CASE branch that checks greater-than-or-equal comparison with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsGreaterOrEqual(object? value)
            => IsGreaterOrEqual(QueryExpression.Param(value));

        /// <summary>
        /// Creates a CASE branch that checks less-than comparison with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsLess(QueryExpression right)
            => Build(Enums.ConditionOperator.LessThan, right);

        /// <summary>
        /// Creates a CASE branch that checks less-than comparison with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsLess(object? value)
            => IsLess(QueryExpression.Param(value));

        /// <summary>
        /// Creates a CASE branch that checks less-than-or-equal comparison with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsLessOrEqual(QueryExpression right)
            => Build(Enums.ConditionOperator.LessThanOrEqual, right);

        /// <summary>
        /// Creates a CASE branch that checks less-than-or-equal comparison with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsLessOrEqual(object? value)
            => IsLessOrEqual(QueryExpression.Param(value));

        /// <summary>
        /// Creates a CASE branch that checks LIKE comparison with an expression.
        /// </summary>
        /// <param name="right">The expression to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsLike(QueryExpression right)
            => Build(Enums.ConditionOperator.Like, right);

        /// <summary>
        /// Creates a CASE branch that checks LIKE comparison with a value.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <returns>A builder for the THEN expression.</returns>
        public CaseThenItem IsLike(object? value)
            => IsLike(QueryExpression.Param(value));

        private CaseThenItem Build(Enums.ConditionOperator op, QueryExpression right)
        {
            var whenExpression = QueryExpression.Binary(_left, op, right);
            return new CaseThenItem(_caseItem, whenExpression);
        }
    }
}
