using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
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
