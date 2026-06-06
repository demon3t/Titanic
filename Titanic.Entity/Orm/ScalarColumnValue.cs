using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Значение обычной скалярной колонки.
    /// </summary>
    public sealed class ScalarColumnValue : ColumnValue
    {
        #region Constructors

        /// <summary>
        /// Создать значение скалярной колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки или SQL-алиас. </param>
        /// <param name="dataValueType"> Тип значения колонки. </param>
        /// <param name="value"> Сырое значение колонки. </param>
        public ScalarColumnValue(string columnName, DataValueType dataValueType, object? value)
            : base(columnName, dataValueType, value)
        {
        }

        #endregion Constructors
    }
}
