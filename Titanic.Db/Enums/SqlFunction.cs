namespace Titanic.Db.Enums
{
    /// <summary>
    /// Канонический список SQL-функций, поддерживаемых проектом.
    /// Рендеринг в SQL-диалект делает движок (<see cref="Abstractions.BaseDbEngine"/>).
    /// Сами строки (<c>"COUNT"</c>, <c>"COALESCE"</c> и т.п.) не должны встречаться
    /// в пользовательском коде или в <see cref="Abstractions.QueryExpression"/>.
    /// </summary>
    public enum SqlFunction
    {
        /// <summary>Не задана, используется для raw- или неизвестных функций через <c>Operator</c>.</summary>
        None = 0,

        /// <summary>COUNT.</summary>
        Count = 1,

        /// <summary>SUM.</summary>
        Sum = 2,

        /// <summary>MIN.</summary>
        Min = 3,

        /// <summary>MAX.</summary>
        Max = 4,

        /// <summary>AVG.</summary>
        Avg = 5,

        /// <summary>LOWER.</summary>
        Lower = 6,

        /// <summary>UPPER.</summary>
        Upper = 7,

        /// <summary>COALESCE.</summary>
        Coalesce = 8,

        /// <summary>Пользовательская функция, имя берётся из <c>QueryExpression.Operator</c>.</summary>
        Custom = 9,
    }
}
