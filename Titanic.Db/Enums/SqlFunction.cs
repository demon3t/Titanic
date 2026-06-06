namespace Titanic.Db.Enums
{
    /// <summary>
    /// Канонический список SQL-функций, поддерживаемых проектом.
    /// Рендеринг в SQL-диалект делает движок (<see cref="Abstractions.BaseDbEngine"/>).
    /// Сами строки (<c>"COUNT"</c>, <c>"COALESCE"</c> и т.п.) НЕ должны встречаться
    /// в пользовательском коде или в <see cref="Abstractions.QueryExpression"/>.
    /// </summary>
    public enum SqlFunction
    {
        /// <summary>Не задана (для raw / неизвестных функций, рендерится через <c>Operator</c>).</summary>
        None = 0,

        /// <summary>COUNT.</summary>
        Count,

        /// <summary>SUM.</summary>
        Sum,

        /// <summary>MIN.</summary>
        Min,

        /// <summary>MAX.</summary>
        Max,

        /// <summary>AVG.</summary>
        Avg,

        /// <summary>LOWER.</summary>
        Lower,

        /// <summary>UPPER.</summary>
        Upper,

        /// <summary>COALESCE.</summary>
        Coalesce,

        /// <summary>Пользовательская функция, имя берётся из <c>QueryExpression.Operator</c>.</summary>
        Custom,
    }
}
