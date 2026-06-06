using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Значение ссылочной колонки с отображаемым значением связанной сущности.
    /// </summary>
    public sealed class ReferenceColumnValue : ColumnValue
    {
        #region Constructors

        /// <summary>
        /// Создать значение ссылочной колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки или SQL-алиас. </param>
        /// <param name="dataValueType"> Тип значения колонки. </param>
        /// <param name="value"> Сырое значение ссылки. </param>
        /// <param name="displayValue"> Отображаемое значение связанной записи. </param>
        public ReferenceColumnValue(string columnName, DataValueType dataValueType, object? value, object? displayValue)
            : base(columnName, dataValueType, value, displayValue)
        {
        }

        #endregion Constructors
    }
}
