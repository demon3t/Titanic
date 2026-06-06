using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Р­С‚Р°Рї РІС‹Р±РѕСЂР° РѕРїРµСЂР°С‚РѕСЂР° СЃСЂР°РІРЅРµРЅРёСЏ РґР»СЏ WHEN.
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

        public CaseThenItem IsContains(QueryExpression right)
            => Build(Enums.ConditionOperator.Contains, right);

        public CaseThenItem IsContains(object? value)
            => IsContains(QueryExpression.Param(value));

        private CaseThenItem Build(Enums.ConditionOperator op, QueryExpression right)
        {
            var whenExpression = QueryExpression.Binary(_left, op, right);
            return new CaseThenItem(_caseItem, whenExpression);
        }
    }
}
