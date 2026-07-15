using System.Data;
using System.Data.Common;
using Titanic.Db.Abstractions;

namespace Titanic.Test.Db
{
    /// <summary>
    /// Тесты методов расширения, которые читают типизированные значения из <see cref="DbDataReader"/>.
    /// </summary>
    public sealed class DbDataReaderExtensionsTests
    {
        /// <summary>
        /// Проверяет, что уже типизированные значения провайдера возвращаются без повторной конвертации.
        /// </summary>
        [Fact]
        public void Get_ShouldReturnTypedValue()
        {
            var expected = Guid.NewGuid();

            using var reader = CreateReader(("id", typeof(Guid), expected));

            Assert.True(reader.Read());
            Assert.Equal(expected, reader.Get<Guid>("id"));
        }

        /// <summary>
        /// Проверяет, что nullable value type сохраняет значение, когда колонка базы данных не равна null.
        /// </summary>
        [Fact]
        public void GetNullable_ShouldReturnValue()
        {
            using var reader = CreateReader(("count", typeof(long), 42L));

            Assert.True(reader.Read());

            var actual = reader.GetNullable<long>("count");

            Assert.Equal(42L, actual);
        }

        /// <summary>
        /// Проверяет, что null-значения базы данных возвращаются как CLR null.
        /// </summary>
        [Fact]
        public void GetNullable_ShouldReturnNull()
        {
            using var reader = CreateReader(("count", typeof(long), DBNull.Value));

            Assert.True(reader.Read());

            var actual = reader.GetNullable<long>("count");

            Assert.Null(actual);
        }

        private static DbDataReader CreateReader((string Name, Type Type, object Value) column)
        {
            var table = new DataTable();
            table.Columns.Add(column.Name, column.Type);

            var row = table.NewRow();
            row[column.Name] = column.Value;
            table.Rows.Add(row);

            return table.CreateDataReader();
        }
    }
}
