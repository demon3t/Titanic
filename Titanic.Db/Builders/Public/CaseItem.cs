using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Р¤Р»СЋРµРЅС‚-Р±РёР»РґРµСЂ РґР»СЏ CASE РІС‹СЂР°Р¶РµРЅРёР№.
    /// РџСЂРёРјРµСЂ:
    /// Func.Case()
    ///     .When(Column.Name("status")).IsEqual(Column.Parameter(1)).Then(Column.Const("one"))
    ///     .Else(Column.Const("other"));
    /// </summary>
    public class CaseItem
    {
        private readonly List<(QueryExpression When, QueryExpression Then)> _branches = new();

        /// <summary>
        /// РќР°С‡Р°С‚СЊ WHEN-РІРµС‚РєСѓ СЃ Р»РµРІРѕРіРѕ РІС‹СЂР°Р¶РµРЅРёСЏ СЃСЂР°РІРЅРµРЅРёСЏ.
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
        /// Р—Р°РІРµСЂС€РёС‚СЊ CASE РІС‹СЂР°Р¶РµРЅРёРµ РІРµС‚РєРѕР№ ELSE.
        /// </summary>
        public QueryExpression Else(QueryExpression elseExpression)
        {
            if (_branches.Count == 0)
            {
                throw new InvalidOperationException("CASE must contain at least one WHEN ... THEN branch");
            }

            return QueryExpression.Case(_branches, elseExpression);
        }
    }
}
