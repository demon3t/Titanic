using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Р­С‚Р°Рї СѓРєР°Р·Р°РЅРёСЏ THEN РІС‹СЂР°Р¶РµРЅРёСЏ.
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
