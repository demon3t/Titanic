using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Значение текстовой колонки.
    /// </summary>
    public sealed class StringColumnValue : ColumnValue
    {
        #region Constructors

        /// <summary>
        /// Создать значение текстовой колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки или SQL-алиас. </param>
        /// <param name="value"> Сырое текстовое значение. </param>
        public StringColumnValue(string columnName, object? value)
            : base(columnName, DataValueType.String, value)
        {
        }

        #endregion Constructors
    }
}
