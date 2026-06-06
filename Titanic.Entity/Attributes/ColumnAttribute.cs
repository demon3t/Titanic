using Titanic.Db.Enums;

namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Колонка.
    /// </summary>
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// Колонка.
        /// </summary>
        public string Column {  get; }

        /// <summary>
        /// Тип данных.
        /// </summary>
        public DataValueType DataValueType { get; }

        /// <summary>
        /// Признак локализуемой текстовой колонки.
        /// </summary>
        public bool IsLocalized { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="column"> Колонка. </param>
        /// <param name="dataValueType"> Тип данных. </param>
        /// <param name="isLocalized"> Признак локализуемой текстовой колонки. </param>
        public ColumnAttribute(string column, DataValueType dataValueType, bool isLocalized = false)
        {
            Column = column;
            DataValueType = dataValueType;
            IsLocalized = isLocalized;
        }
    }
}
