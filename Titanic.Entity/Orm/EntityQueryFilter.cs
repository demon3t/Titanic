using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Описание leaf-фильтра EntitySchemaQuery.
    /// </summary>
    public sealed class EntityQueryFilter : EntityQueryFilterNode
    {
        #region Constructors

        internal EntityQueryFilter(string path, ConditionOperator comparisonType)
        {
            Path = path;
            ComparisonType = comparisonType;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// ORM-путь колонки фильтра.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Тип сравнения.
        /// </summary>
        public ConditionOperator ComparisonType { get; }

        /// <summary>
        /// Первое значение фильтра.
        /// </summary>
        public object? Value { get; private set; }

        /// <summary>
        /// Второе значение фильтра для диапазона.
        /// </summary>
        public object? SecondValue { get; private set; }

        /// <summary>
        /// Признак отрицания условия.
        /// </summary>
        public bool IsNot { get; private set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Установить значение фильтра.
        /// </summary>
        /// <param name="value"> Значение фильтра. </param>
        /// <returns> Текущий фильтр. </returns>
        public EntityQueryFilter WithValue(object? value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Установить диапазон фильтра.
        /// </summary>
        /// <param name="from"> Нижняя граница. </param>
        /// <param name="to"> Верхняя граница. </param>
        /// <returns> Текущий фильтр. </returns>
        public EntityQueryFilter WithRange(object? from, object? to)
        {
            Value = from;
            SecondValue = to;
            return this;
        }

        /// <summary>
        /// Инвертировать фильтр.
        /// </summary>
        /// <returns> Текущий фильтр. </returns>
        public EntityQueryFilter Not()
        {
            IsNot = true;
            return this;
        }

        #endregion Public Methods
    }
}
