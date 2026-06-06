using Titanic.Db;
using Titanic.Db.Abstractions;
namespace Titanic.Test.Db
{
    /// <summary>
    /// Тесты SQL builder для PostgreSQL диалекта.
    /// </summary>
    public class PostgreSqlQueryBuilderTests : IClassFixture<DbManagerFixture>
    {
        #region Поля

        private readonly BaseDatabase _provider;

        public PostgreSqlQueryBuilderTests(DbManagerFixture fixture)
        {
            // DbManager — статический, инициализируется фикстурой.
            // Обёртка PostgresDatabase с атрибутом [DatabaseConnection("test")]
            // автоматически регистрируется в DbManager при первом вызове Get<T>().
            _provider = DbManager.Get<PostgresDatabase>();
        }

        #endregion Поля

        #region SELECT

        [Fact]
        public void Select_ShouldBuildSqlWithJoinWhereOrderAndPaging()
        {
            var query = _provider.Select()
                .Column(Column.Name("u", "id").As("user_id"))
                .Column(Column.Name("u", "name"))
                .Distinct()
                .From("public.users").As("u")
                .InnerJoin("roles")
                    .As("r")
                    .On("u", "role_id").IsEqual("r", "id")
                .Where("u", "is_active").IsEqual(true)
                .And("u", "name").IsContains("ivan")
                .OrderBy("u", "id")
                .Limit(20)
                .Page(2, 20);

            var build = query.Build();

            AssertSql(
                """
                SELECT DISTINCT
                	"u"."id" AS "user_id", "u"."name"
                FROM
                	"public"."users" AS "u"
                INNER JOIN
                	"roles" AS "r"
                ON
                	("u"."role_id" = "r"."id")
                WHERE
                	(("u"."is_active" = @p0) AND (UPPER("u"."name") LIKE UPPER(@p1)))
                ORDER BY
                	"u"."id" ASC
                LIMIT
                	@p2
                OFFSET
                	@p3
                """,
                build.Sql);
            AssertParameters(build, true, "%ivan%", 20, 20);
        }

        [Fact]
        public void Select_ShouldBuildGroupByHavingAndThenBy()
        {
            var query = _provider.Select()
                .Column("u", "role_id")
                .Column(Func.Avg("u", "age").As("avg_age"))
                .From("public.users").As("u")
                .GroupBy("u", "role_id")
                .Having(Func.Count(Column.Asterisk()))
                .IsGreaterThan(1)
                .OrderBy("u", "role_id", desc: true)
                .ThenBy("avg_age")
                .Limit(5)
                    .Skip(10);

            var build = query.Build();

            AssertSql(
                """
                SELECT
                	"u"."role_id", AVG("u"."age") AS "avg_age"
                FROM
                	"public"."users" AS "u"
                GROUP BY
                	"u"."role_id"
                HAVING
                	(COUNT(*) > @p0)
                ORDER BY
                	"u"."role_id" DESC, "avg_age" ASC
                LIMIT
                	@p1
                OFFSET
                	@p2
                """,
                build.Sql);
            AssertParameters(build, 1, 5, 10);
        }

        [Fact]
        public void Select_ShouldBuildUnionAndUnionAll()
        {
            var first = _provider.Select()
                .Column("id")
                .From("public.users").As("u")
                .Where("is_active").IsEqual(Column.Parameter(true));

            var second = _provider.Select()
                .Column("id")
                .From("public.archived_users").As("a")
                .Where("is_active").IsEqual(Column.Parameter(false));

            var third = _provider.Select()
                .Column("id")
                .From("public.deleted_users").As("d");

            var build = first.Union(second).UnionAll(third).Build();

            AssertSql(
                """
                ((SELECT
                	"id"
                FROM
                	"public"."users" AS "u"
                WHERE
                	("is_active" = @p0))
                UNION
                (SELECT
                	"id"
                FROM
                	"public"."archived_users" AS "a"
                WHERE
                	("is_active" = @p1)))
                UNION ALL
                (SELECT
                	"id"
                FROM
                	"public"."deleted_users" AS "d")
                """,
                build.Sql);
            AssertParameters(build, true, false);
        }

        #endregion SELECT

        #region WHERE

        [Fact]
        public void WhereBuilder_ShouldBuildOperatorsAndGroups()
        {
            var query = _provider.Select()
                .Column("u", Column.Asterisk())
                .From("public.users").As("u")
                .Where()
                    .AndOpen()
                        .GreaterThanOrEqual("u", "age", Column.Parameter(18))
                        .LessThan("u", "age", Column.Parameter(65))
                    .Close()
                    .Or()
                    .AndOpen()
                        .IsNull("u", "deleted_on")
                        .NotContains("u", "name", Column.Parameter("test"))
                    .Close()
                .End();

            var build = query.Build();

            AssertSql(
                """
                SELECT
                	"u".*
                FROM
                	"public"."users" AS "u"
                WHERE
                	((("u"."age" >= @p0) AND ("u"."age" < @p1)) OR (("u"."deleted_on" IS NULL) AND NOT ((UPPER("u"."name") LIKE UPPER(@p2)))))
                """,
                build.Sql);
            AssertParameters(build, 18, 65, "%test%");
        }

        #endregion WHERE

        #region UPDATE

        [Fact]
        public void Update_ShouldBuildParameterizedSql()
        {
            var query = _provider.Update("public.users").As("u")
                .Set("u", "name", Column.Parameter("Ivan"))
                .Set("updated_on", Func.Custom("NOW"))
                .Where("u", "id").IsEqual(Column.Parameter(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")))
                .Returning("u", "id");

            var build = query.Build();

            AssertSql(
                """
                UPDATE
                	"public"."users" AS "u"
                SET
                	"u"."name" = @p0,
                	"updated_on" = NOW()
                WHERE
                	("u"."id" = @p1)
                RETURNING
                	"u"."id"
                """,
                build.Sql);
            AssertParameters(build, "Ivan", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        }

        #endregion UPDATE

        #region DELETE

        [Fact]
        public void Delete_ShouldBuildParameterizedSql()
        {
            var query = _provider.Delete("users")
                .As("u")
                .Using("roles", "r")
                .Where("u", "role_id").IsEqual("r", "id")
                .And("r", "name").IsEqual(Column.Parameter("guest"))
                .Returning("u", "id");

            var build = query.Build();

            AssertSql(
                """
                DELETE FROM
                	"users" AS "u"
                USING
                	"roles" AS "r"
                WHERE
                	(("u"."role_id" = "r"."id") AND ("r"."name" = @p0))
                RETURNING
                	"u"."id"
                """,
                build.Sql);
            AssertParameters(build, "guest");
        }

        #endregion DELETE

        #region INSERT

        [Fact]
        public void InsertSelect_ShouldBuildValuesInsertWithConflictAndReturning()
        {
            var id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var query = _provider.Insert("users")
                .Into("users")
                .SetColumns("id", "name")
                .Values(Column.Parameter(id), Column.Parameter("Ivan"))
                .OnConflictDoNothing("id")
                .Returning("id");

            var build = query.Build();

            AssertSql(
                """
                INSERT INTO
                	"users"
                (
                	"id",
                	"name"
                )
                VALUES
                	(@p0, @p1)
                ON CONFLICT ("id") DO NOTHING
                RETURNING
                	"id"
                """,
                build.Sql);
            AssertParameters(build, id, "Ivan");
        }

        [Fact]
        public void InsertSelect_ShouldBuildConflictDoUpdate()
        {
            var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var query = _provider.Insert("users")
                .Into("users")
                .SetColumns("id", "name", "updated_on")
                .Values(Column.Parameter(id), Column.Parameter("Ivan"), Func.Custom("NOW"))
                .OnConflict("id")
                .DoUpdateSetExcluded("name")
                .DoUpdateSet("updated_on", Func.Custom("NOW"))
                .Returning("id");

            var build = query.Build();

            AssertSql(
                """
                INSERT INTO
                	"users"
                (
                	"id",
                	"name",
                	"updated_on"
                )
                VALUES
                	(@p0, @p1, NOW())
                ON CONFLICT ("id") DO UPDATE
                SET
                	"name" = "excluded"."name",
                	"updated_on" = NOW()
                RETURNING
                	"id"
                """,
                build.Sql);
            AssertParameters(build, id, "Ivan");
        }

        [Fact]
        public void InsertSelect_ShouldBuildInsertFromSelect()
        {
            var source = _provider.Select()
                .Column("id")
                .Column("name")
                .From("pending_users").As("p")
                .Where("name").IsNotNull()
                .And("status").IsEqual(Column.Parameter("approved"));

            var query = _provider.Insert("users")
                .Into("users")
                .SetColumns("id", "name")
                .FromSelect(source);

            var build = query.Build();

            AssertSql(
                """
                INSERT INTO
                	"users"
                (
                	"id",
                	"name"
                )
                SELECT
                	"id", "name"
                FROM
                	"pending_users" AS "p"
                WHERE
                	(("name" IS NOT NULL) AND ("status" = @p0))
                """,
                build.Sql);
            AssertParameters(build, "approved");
        }

        #endregion INSERT

        #region SAFETY

        [Fact]
        public void Build_ShouldFormatConstValueUsingEngine()
        {
            var query = _provider.Select()
                .Column(Column.Asterisk())
                .From("public.users").As("u")
                .Where("name").IsEqual(Column.Const("Ivan"));

            var build = query.Build();

            AssertSql(
                """
                SELECT
                	*
                FROM
                	"public"."users" AS "u"
                WHERE
                	("name" = 'Ivan')
                """,
                build.Sql);
            Assert.Empty(build.Parameters);
        }

        [Fact]
        public void Select_ShouldBuildSubQueryAsColumnAndInFilter()
        {
            var lastLoginSubQuery = _provider.Select()
                .Column(Func.Max("l", "created_on"))
                .From("public.logins").As("l")
                .Where("l", "user_id").IsEqual("u", "id");

            var roleSubQuery = _provider.Select()
                .Column("r", "id")
                .From("public.roles").As("r")
                .Where("r", "is_active").IsEqual(Column.Parameter(true));

            var query = _provider.Select()
                .Column("u", "id")
                .Column(Column.SubQuery(lastLoginSubQuery).As("last_login"))
                .From("public.users").As("u")
                .Where("u", "role_id").In(roleSubQuery);

            var build = query.Build();

            AssertSql(
                """
                SELECT
                	"u"."id", (SELECT
                	MAX("l"."created_on")
                FROM
                	"public"."logins" AS "l"
                WHERE
                	("l"."user_id" = "u"."id")) AS "last_login"
                FROM
                	"public"."users" AS "u"
                WHERE
                	("u"."role_id" IN (SELECT
                	"r"."id"
                FROM
                	"public"."roles" AS "r"
                WHERE
                	("r"."is_active" = @p0)))
                """,
                build.Sql);
            AssertParameters(build, true);
        }

        [Fact]
        public void Func_ShouldBuildAggregateCoalesceAndCaseExpressions()
        {
            var query = _provider.Select()
                .Column(Func.Min(Column.Name("age")).As("min_age"))
                .Column(Func.Max(Column.Name("age")).As("max_age"))
                .Column(Func.Avg(Column.Name("age")).As("avg_age"))
                .Column(Func.Coalesce(Column.Name("name"), Column.Const("unknown")).As("display_name"))
                .Column(Func.Case()
                    .When(Column.Name("is_active")).IsEqual(Column.Parameter(true)).Then(Column.Const("active"))
                    .Else(Column.Const("inactive")).As("state"))
                .From("public.users").As("u");

            var build = query.Build();

            AssertSql(
                """
                SELECT
                	MIN("age") AS "min_age", MAX("age") AS "max_age", AVG("age") AS "avg_age", COALESCE("name", 'unknown') AS "display_name", CASE WHEN ("is_active" = @p0) THEN 'active' ELSE 'inactive' END AS "state"
                FROM
                	"public"."users" AS "u"
                """,
                build.Sql);
            AssertParameters(build, true);
        }

        [Fact]
        public void Func_Case_ShouldBuildMultipleWhenThenElse()
        {
            var subSelect = _provider.Select()
                .Column(Func.Max(Column.Name("n2", "number")))
                .From("public.numbers").As("n2");

            var query = _provider.Select()
                .Column(Func.Case()
                    .When(Column.Name("number")).IsEqual(Column.Parameter(1)).Then(Column.Const("one"))
                    .When(Column.Name("number")).IsEqual(Column.SubQuery(subSelect)).Then(Column.Const("select"))
                    .When(Column.Const(2)).IsEqual(Column.Const(3)).Then(Column.Const("three"))
                    .Else(Column.Const("undefined")).As("word"))
                .From("public.numbers").As("n");

            var build = query.Build();

            AssertSql(
                """
                SELECT
                	CASE WHEN ("number" = @p0) THEN 'one' WHEN ("number" = (SELECT
                	MAX("n2"."number")
                FROM
                	"public"."numbers" AS "n2")) THEN 'select' WHEN (2 = 3) THEN 'three' ELSE 'undefined' END AS "word"
                FROM
                	"public"."numbers" AS "n"
                """,
                build.Sql);
            AssertParameters(build, 1);
        }

        #endregion SAFETY

        #region Вспомогательные методы

        private static void AssertParameters(QueryBuildResult build, params object?[] expectedValues)
        {
            Assert.Equal(expectedValues.Length, build.Parameters.Count);

            for (var i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal($"p{i}", build.Parameters[i].Name);
                Assert.Equal(expectedValues[i], build.Parameters[i].Value);
            }
        }

        private static void AssertSql(string expected, string actual)
        {
            Assert.Equal(NormalizeSql(expected), NormalizeSql(actual));
        }

        private static string NormalizeSql(string sql)
        {
            return sql.Replace("\r\n", "\n").Trim();
        }

        #endregion Вспомогательные методы
    }
}
