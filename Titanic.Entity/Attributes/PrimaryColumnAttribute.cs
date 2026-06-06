using Titanic.Db.Enums;

namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Первичный ключ.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryColumnAttribute : ColumnAttribute
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="column"> Колонка.</param>
        /// <param name="dataValueType"> Тип данных. </param>
        public PrimaryColumnAttribute(string column, DataValueType dataValueType = DataValueType.Guid)
            : base(column, dataValueType)
        {

        }
    }
}
