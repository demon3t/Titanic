namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Преобразует значения, полученные из ADO.NET reader-ов и команд, в ожидаемый CLR-тип.
    /// </summary>
    internal static class DbValueConverter
    {
        /// <summary>
        /// Преобразовать значение базы данных в ожидаемый CLR-тип без повторной конвертации уже типизированных значений.
        /// </summary>
        /// <typeparam name="T"> Целевой CLR-тип. </typeparam>
        /// <param name="value"> Значение, возвращенное провайдером базы данных. </param>
        /// <returns> Преобразованное значение или значение по умолчанию, если значение базы данных равно null. </returns>
        internal static T? ConvertTo<T>(object? value)
        {
            if (value == null || value is DBNull)
            {
                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType);
        }
    }
}
