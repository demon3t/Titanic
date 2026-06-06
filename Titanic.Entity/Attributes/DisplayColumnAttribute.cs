using Titanic.Db.Enums;

namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Отображаемая колонка.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayColumnAttribute : ColumnAttribute
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="column"> Колонка. </param>
        /// <param name="dataValueType"> Тип данных. </param>
        /// <param name="isLocalized"> Признак локализуемой текстовой колонки. </param>
        public DisplayColumnAttribute(
            string column,
            DataValueType dataValueType = DataValueType.String,
            bool isLocalized = false)
            : base(column, dataValueType, isLocalized)
        {

        }
    }
}
