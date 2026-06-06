using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Базовое значение колонки сущности с сырым значением и опциональным отображаемым значением.
    /// </summary>
    public abstract class ColumnValue
    {
        #region Constructors

        /// <summary>
        /// Создать значение колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки или SQL-алиас. </param>
        /// <param name="dataValueType"> Тип значения колонки. </param>
        /// <param name="value"> Сырое значение колонки. </param>
        /// <param name="displayValue"> Отображаемое значение колонки. </param>
        protected ColumnValue(string columnName, DataValueType dataValueType, object? value, object? displayValue = null)
        {
            ColumnName = string.IsNullOrWhiteSpace(columnName)
                ? throw new ArgumentException("Column name is empty", nameof(columnName))
                : columnName;
            DataValueType = dataValueType;
            Value = value;
            DisplayValue = displayValue;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя колонки или SQL-алиас, по которому значение хранится в сущности.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Тип значения колонки.
        /// </summary>
        public DataValueType DataValueType { get; }

        /// <summary>
        /// Сырое значение колонки из БД или установленное пользователем значение.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Отображаемое значение колонки, например display-колонка связанной сущности.
        /// </summary>
        public object? DisplayValue { get; set; }

        #endregion Properties
    }
}
