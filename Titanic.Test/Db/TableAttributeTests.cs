using System.Data.Common;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Attributes;
using Titanic.Db.PosgreSql;

namespace Titanic.Test.Db
{
    /// <summary>
    /// Тесты DDL-создания таблиц и индексов по атрибутам.
    /// </summary>
    public sealed class TableAttributeTests
    {
        #region Tests

        [Fact]
        public void Table_CreateIfNotExists_ShouldCreateTableAndIndexesFromAttributes()
        {
            var provider = new CapturingDbProvider();

            provider.Table<AttributeUserTable>().CreateIfNotExists<AttributeUserTable>();

            Assert.Equal(4, provider.SqlHistory.Count);
            AssertSql(
                """
                CREATE TABLE IF NOT EXISTS "public"."attribute_users" (
                	"id" SERIAL,
                	"name" VARCHAR(128) NOT NULL,
                	"email" VARCHAR(255) NOT NULL UNIQUE,
                	"department_id" INTEGER REFERENCES public.departments(id),
                	PRIMARY KEY ("id"),
                	CONSTRAINT "UX_attribute_users_name_email" UNIQUE ("name", "email")
                )
                """,
                provider.SqlHistory[0]);
            Assert.Equal(
                """CREATE INDEX IF NOT EXISTS "IX_attribute_users_department_name" ON "public"."attribute_users" ("department_id", "name")""",
                provider.SqlHistory[1]);
            Assert.Equal(
                """CREATE INDEX IF NOT EXISTS "IX_attribute_users_name" ON "public"."attribute_users" ("name")""",
                provider.SqlHistory[2]);
            Assert.Equal(
                """CREATE UNIQUE INDEX IF NOT EXISTS "IX_attribute_users_email" ON "public"."attribute_users" ("email")""",
                provider.SqlHistory[3]);
        }

        [Fact]
        public void Table_CreateIndexes_ShouldUsePropertyColumnNameWhenIndexColumnsAreEmpty()
        {
            var provider = new CapturingDbProvider();

            var count = provider.Table<AttributeUserTable>().CreateIndexes<AttributeUserTable>();

            Assert.Equal(3, count);
            Assert.Contains(
                """CREATE INDEX IF NOT EXISTS "IX_attribute_users_name" ON "public"."attribute_users" ("name")""",
                provider.SqlHistory);
        }

        #endregion Tests

        #region Helpers

        private static void AssertSql(string expected, string actual)
        {
            Assert.Equal(
                NormalizeSql(expected),
                NormalizeSql(actual));
        }

        private static string NormalizeSql(string sql)
        {
            return sql.Replace("\r\n", "\n").Trim();
        }

        #endregion Helpers

        #region Test Models

        [DbTable("public.attribute_users")]
        [DbTableConstraint("UX_attribute_users_name_email", Table.ConstraintType.Unique, "name", "email")]
        [DbIndex("IX_attribute_users_department_name", "department_id", "name")]
        private sealed class AttributeUserTable
        {
            [DbColumn("id", Table.ColumnType.Serial, PrimaryKey = true)]
            public int Id { get; set; }

            [DbColumn("name", Table.ColumnType.VarChar, Length = 128, NotNull = true)]
            [DbIndex("IX_attribute_users_name")]
            public string Name { get; set; } = string.Empty;

            [DbColumn("email", Table.ColumnType.VarChar, Length = 255, NotNull = true, Unique = true)]
            [DbIndex("IX_attribute_users_email", Unique = true)]
            public string Email { get; set; } = string.Empty;

            [DbColumn("department_id", Table.ColumnType.Integer, References = "public.departments(id)")]
            public int? DepartmentId { get; set; }
        }

        #endregion Test Models

        #region Test Provider

        private sealed class CapturingDbProvider : BaseDbProvider
        {
            private readonly List<string> _sqlHistory = [];

            public CapturingDbProvider()
                : base("mock", new PostgresEngine())
            {
            }

            public IReadOnlyList<string> SqlHistory => _sqlHistory;

            public override int Execute(string sql, IEnumerable<QueryParameter>? parameters = null)
            {
                _sqlHistory.Add(sql);
                return 1;
            }

            protected override DbConnection CreateConnection()
            {
                throw new NotSupportedException();
            }

            protected override DbParameter CreateParameter(QueryParameter parameter)
            {
                throw new NotSupportedException();
            }
        }

        #endregion Test Provider
    }
}
