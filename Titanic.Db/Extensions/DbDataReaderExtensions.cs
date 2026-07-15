using System.Data.Common;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Методы расширения для <see cref="DbDataReader"/>.
    /// Позволяют читать данные по имени колонки.
    /// </summary>
    public static class DbDataReaderExtensions
    {
        /// <summary>
        /// Получить значение колонки по имени.
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="reader"> Чтение данных. </param>
        /// <param name="columnName"> Имя колонки. </param>
        public static T Get<T>(this DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return default!;
            }

            var value = reader.GetValue(ordinal);
            return DbValueConverter.ConvertTo<T>(value)!;
        }

        /// <summary>
        /// Получить значение колонки по имени (nullable).
        /// </summary>
        /// <typeparam name="T"> Тип значения. </typeparam>
        /// <param name="reader"> Чтение данных. </param>
        /// <param name="columnName"> Имя колонки. </param>
        public static T? GetNullable<T>(this DbDataReader reader, string columnName) where T : struct
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            return DbValueConverter.ConvertTo<T>(value);
        }
    }
}
