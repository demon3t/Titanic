namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Тип агрегатной функции EntitySchemaQuery.
    /// </summary>
    public enum EntityAggregationType
    {
        /// <summary>
        /// Обычная колонка без агрегации.
        /// </summary>
        None = 0,

        /// <summary>
        /// COUNT.
        /// </summary>
        Count = 1,

        /// <summary>
        /// SUM.
        /// </summary>
        Sum = 2,

        /// <summary>
        /// AVG.
        /// </summary>
        Avg = 3,

        /// <summary>
        /// MIN.
        /// </summary>
        Min = 4,

        /// <summary>
        /// MAX.
        /// </summary>
        Max = 5
    }
}
