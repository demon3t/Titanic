using Titanic.Db.Abstractions;

namespace Titanic.Db
{
    internal static class QueryExpressionAliasExtensions
    {
        /// <summary>
        /// Применить алиас к выражению, если алиас был передан и содержит значимый текст.
        /// </summary>
        /// <param name="expression">Выражение, к которому нужно применить алиас.</param>
        /// <param name="alias">Опциональный алиас результата выражения.</param>
        /// <returns>Исходное выражение без изменений либо выражение с установленным алиасом.</returns>
        public static QueryExpression AsIfNotEmpty(this QueryExpression expression, string? alias)
        {
            return string.IsNullOrWhiteSpace(alias) ? expression : expression.As(alias);
        }
    }
}
