using Titanic.Db.Enums;

namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Ссылочная колонка.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReferenceColumnAttribute : ColumnAttribute
    {
        /// <summary>
        /// Таблица, на которую идет ссылка.
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="column"> Колонка. </param>
        /// <param name="column"> Таблица, на которую идет ссылка. </param>
        /// <param name="dataValueType"> Тип данных. </param>
        public ReferenceColumnAttribute(string column, string table, DataValueType dataValueType = DataValueType.Guid) 
            : base(column, dataValueType)
        {
             Table = table;
        }
    }
}
