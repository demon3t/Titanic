using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity;
using Titanic.Entity.Interfaces;
using EntityManager = Titanic.Entity.EntityManager;
using Titanic.Entity.Orm;
using Titanic.Entity.WebApplication.Configuration;
using Titanic.Test.Db;
using Titanic.Test.Db.Integration;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// РўРµСЃС‚С‹ ORM-Р±РёР»РґРµСЂР° СЃСѓС‰РЅРѕСЃС‚РµР№.
    /// </summary>
    public class EntitySelectBuilderTests : IClassFixture<DbManagerFixture>
    {
        private readonly BaseDbProvider _provider;

        public EntitySelectBuilderTests(DbManagerFixture fixture)
        {
            _provider = DbManager.GetProvider();
        }

        [Fact]
        public void EntityManager_Query_ShouldRequireUserConnection()
        {
            Assert.Throws<ArgumentNullException>(
                () => EntityManager.Query<OrmEmployeeEntity>(_provider, null!));
        }

        [Fact]
        public void EntitySelectBuilder_ShouldRequireUserConnection()
        {
            Assert.Throws<ArgumentNullException>(
                () => new EntitySelectBuilder<OrmEmployeeEntity>(_provider, null!));
        }

        [Fact]
        public void ESQJsonModel_ToESQ_ShouldRequireUserConnection()
        {
            var model = new ESQJsonModel
            {
                TableName = "employees"
            };

            Assert.Throws<ArgumentNullException>(() => model.ToESQ(_provider, null!));
        }

        [Fact]
        public void EntityManager_Initialize_ShouldRegisterManagerAndCreateEntitySchemaQuery()
        {
            EntityManager.Initialize(new EntityManagerConfig
            {
                Managers =
                [
                    new EntityManagerSettings
                    {
                        Name = "TestPostgres",
                        DbProviderName = "TestPostgres",
                        Api = new EntityManagerApiSettings
                        {
                            AutoRegisterEndpoint = true,
                            Path = "/api/entity/test",
                            AuthorizationHeaderName = "X-Test-Entity-Key",
                            AuthorizationProviderType = "Titanic.Entity.WebApplication.Api.HeaderEntityApiAuthorizationProvider, Titanic.Entity"
                        },
                        Options = new EntityManagerOptions
                        {
                            MaxReadRowCount = 25
                        },
                        ValidateDatabaseSchemaOnCompile = false
                    }
                ]
            });

            var manager = EntityManager.GetManager<EntityDbManager>();
            var esq = EntityManager.Query<OrmEmployeeEntity>(OrmTestUserConnection.Create());

            Assert.Same(manager, EntityManager.Get<EntityDbManager>());
            Assert.IsType<EntitySchemaQuery<OrmEmployeeEntity>>(esq);
            Assert.True(manager.Api.AutoRegisterEndpoint);
            Assert.Equal("/api/entity/test", manager.Api.Path);
            Assert.Equal("X-Test-Entity-Key", manager.Api.AuthorizationHeaderName);
            Assert.Equal(
                "Titanic.Entity.WebApplication.Api.HeaderEntityApiAuthorizationProvider, Titanic.Entity",
                manager.Api.AuthorizationProviderType);
            Assert.Equal(25, manager.Options.MaxReadRowCount);
            Assert.False(manager.ValidateDatabaseSchemaOnCompile);
            Assert.Equal(25, esq.MaxReadRowCount);
        }

        [Fact]
        public void EntitySchemaQuery_MaxReadRowCount_ShouldLimitReturnedRows()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create());
            esq.AddColumn("Name");
            esq.RowCount = 100;
            esq.MaxReadRowCount = 10;

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"employees" AS "t0"
                LIMIT
                	@p0
                """,
                build.Sql);
            AssertParameters(build, 10);
        }

        [Fact]
        public void EntitySelectBuilder_ShouldBuildLeftJoinPath()
        {
            var build = EntityManager.Select<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create())
                .AddColumn("Name")
                .AddColumn("DepartmentId.Name")
                .Where("DepartmentId.Name").IsEqual(Column.Parameter("Engineering"))
                .Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name", "t1"."name" AS "DepartmentId_Name"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                WHERE
                	("t1"."name" = @p0)
                """,
                build.Sql);

            AssertParameters(build, "Engineering");
        }

        [Fact]
        public void EntitySelectBuilder_ShouldBuildRightJoinPath()
        {
            var build = EntityManager.Select<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create())
                .AddColumn("Name")
                .AddColumn("[EmployeeId:Id:Id].City")
                .Where("[EmployeeId:Id:Id].City").IsLike("Moscow%")
                .OrderBy("[EmployeeId:Id:Id].City")
                .Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name", "t1"."city" AS "EmployeeId_Id_Id__City"
                FROM
                	"employees" AS "t0"
                RIGHT JOIN
                	"addresses" AS "t1"
                ON
                	("t1"."employee_id" = "t0"."id")
                WHERE
                	("t1"."city" LIKE @p0)
                ORDER BY
                	"t1"."city" ASC
                """,
                build.Sql);

            AssertParameters(build, "Moscow%");
        }

        [Fact]
        public void EntitySelectBuilder_ShouldMaterializeDictionaryBackedRecord()
        {
            var record = EntityManager.Select<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create())
                .AddColumn("Name")
                .AddColumn("DepartmentId.Name")
                .CreateRecord(new Dictionary<string, object?>
                {
                    ["Name"] = "Ivan",
                    ["DepartmentId_Name"] = "Engineering"
                });

            Assert.Equal("Ivan", record.Get<string>("Name"));
            Assert.Equal("Engineering", record.Get<string>("DepartmentId.Name"));
            Assert.Equal("Engineering", record["DepartmentId_Name"]);
            Assert.True(record.Contains("DepartmentId.Name"));
        }

        [Fact]
        public void EntitySelectBuilder_ReferenceColumn_ShouldSelectDisplayColumnImplicitly()
        {
            var build = EntityManager.Select<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create())
                .AddColumn("DepartmentId")
                .Build();

            AssertSql(
                """
                SELECT
                	"t0"."department_id" AS "DepartmentId", "t1"."name" AS "DepartmentId_DisplayValue"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                """,
                build.Sql);
        }

        [Fact]
        public void Entity_ShouldStoreReferenceColumnValueWithDisplayValue()
        {
            var record = EntityManager.Select<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create())
                .AddColumn("DepartmentId")
                .CreateRecord(new Dictionary<string, object?>
                {
                    ["DepartmentId"] = 10,
                    ["DepartmentId_DisplayValue"] = "Engineering"
                });

            var value = Assert.IsType<ReferenceColumnValue>(record.Values["DepartmentId"]);
            Assert.Equal(10, value.Value);
            Assert.Equal("Engineering", value.DisplayValue);
            Assert.Equal(10, record.Get<int>("DepartmentId"));
            Assert.Equal("Engineering", record.GetDisplayValue<string>("DepartmentId"));
            Assert.False(record.Values.ContainsKey("DepartmentId_DisplayValue"));
        }

        [Fact]
        public void ESQ_ShouldBuildColumnsAndFilters()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create());
            esq.AddPrimaryColumn();
            esq.AddDisplayColumn("EmployeeName");
            esq.AddColumn("DepartmentId.Name", "DepartmentName");
            esq.AddFilter(ConditionOperator.Equal, "DepartmentId.Name", "Engineering");
            esq.AddFilter(ConditionOperator.Like, "Email", "%@t.com");
            esq.RowCount = 10;

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."id" AS "Id", "t0"."name" AS "EmployeeName", "t1"."name" AS "DepartmentName"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                WHERE
                	(("t1"."name" = @p0) AND ("t0"."email" LIKE @p1))
                LIMIT
                	@p2
                """,
                build.Sql);

            AssertParameters(build, "Engineering", "%@t.com", 10);
        }

        [Fact]
        public void ESQ_NestedFilterGroup_ShouldBuildGroupedWhere()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create());
            esq.AddColumn("Name");
            esq.Filters.Add("Name", ConditionOperator.Like, "A%");
            var group = esq.Filters.AddGroup(EntityLogicalOperation.Or);
            group.Add("Email", ConditionOperator.Like, "%@t.com");
            group.Add("Salary", ConditionOperator.GreaterThanOrEqual, 1000);

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"employees" AS "t0"
                WHERE
                	(("t0"."name" LIKE @p0) AND (("t0"."email" LIKE @p1) OR ("t0"."salary" >= @p2)))
                """,
                build.Sql);

            AssertParameters(build, "A%", "%@t.com", 1000);
        }

        [Fact]
        public void ESQ_ShouldBuildAllSchemaColumnsAndBetweenFilter()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create());
            esq.AddAllSchemaColumns();
            esq.AddBetweenFilter("Id", 10, 20);
            esq.SkipRowCount = 5;
            esq.RowCount = 3;

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."id" AS "Id", "t0"."name" AS "Name", "t0"."email" AS "Email", "t0"."department_id" AS "DepartmentId", "t1"."name" AS "DepartmentId_DisplayValue", "t0"."salary" AS "Salary", "t0"."is_active" AS "IsActive"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                WHERE
                	(("t0"."id" >= @p0) AND ("t0"."id" <= @p1))
                LIMIT
                	@p2
                OFFSET
                	@p3
                """,
                build.Sql);

            AssertParameters(build, 10, 20, 3, 5);
        }

        [Fact]
        public void ESQJsonModel_EmptyColumns_ShouldSelectAllSchemaColumns()
        {
            var json = """
            {
              "tableName": "departments",
              "rowCount": 10,
              "columns": []
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."id" AS "Id", "t0"."name" AS "Name", "t0"."description" AS "Description"
                FROM
                	"departments" AS "t0"
                LIMIT
                	@p0
                """,
                build.Sql);

            AssertParameters(build, 10);
        }

        [Fact]
        public void ESQJsonModel_AllColumnsMarker_ShouldSelectAllSchemaColumns()
        {
            var json = """
            {
              "tableName": "departments",
              "rowCount": 10,
              "columns": [
                { "path": "*" }
              ]
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."id" AS "Id", "t0"."name" AS "Name", "t0"."description" AS "Description"
                FROM
                	"departments" AS "t0"
                LIMIT
                	@p0
                """,
                build.Sql);

            AssertParameters(build, 10);
        }

        [Fact]
        public void ESQ_SkipRow_ShouldBuildOffset()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create());
            esq.AddDisplayColumn();
            esq.OrderBy("Name");
            esq.RowCount = 2;
            esq.SkipRow = 4;

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"employees" AS "t0"
                ORDER BY
                	"t0"."name" ASC
                LIMIT
                	@p0
                OFFSET
                	@p1
                """,
                build.Sql);

            AssertParameters(build, 2, 4);
        }

        [Fact]
        public void EntitySelectBuilder_ShouldBuildFromAbstractEntityType()
        {
            var build = EntityManager.Select(typeof(OrmAbstractEmployeeEntity), _provider, OrmTestUserConnection.Create())
                .AddColumn("Name")
                .AddColumn("DepartmentId.Name")
                .Where("DepartmentId.Name").IsEqual("Engineering")
                .Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name", "t1"."name" AS "DepartmentId_Name"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                WHERE
                	("t1"."name" = @p0)
                """,
                build.Sql);

            AssertParameters(build, "Engineering");
        }

        [Fact]
        public void ESQ_ShouldBuildFromTableName()
        {
            var esq = EntityManager.Query("employees", _provider, OrmTestUserConnection.Create());
            esq.AddDisplayColumn();
            esq.AddFilter(ConditionOperator.Like, "Email", "%@t.com");

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"employees" AS "t0"
                WHERE
                	("t0"."email" LIKE @p0)
                """,
                build.Sql);

            AssertParameters(build, "%@t.com");
        }

        [Fact]
        public void ESQJsonModel_ShouldRestoreESQ()
        {
            var model = new ESQJsonModel
            {
                TableName = "employees",
                Columns =
                [
                    new() { Path = "Name" },
                    new() { Path = "DepartmentId.Name", Alias = "DepartmentName" }
                ],
                Filters = new ESQFilterCollectionJsonModel
                {
                    Items =
                    [
                        new()
                        {
                            Path = "DepartmentId.Name",
                            ComparisonType = ConditionOperator.Equal,
                            Value = "Engineering"
                        },
                        new()
                        {
                            Path = "Email",
                            ComparisonType = ConditionOperator.Like,
                            Value = "%@t.com"
                        }
                    ]
                },
                Orders =
                [
                    new() { Path = "Name", Desc = true }
                ],
                RowCount = 5
            };

            var build = model.ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name", "t1"."name" AS "DepartmentName"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                WHERE
                	(("t1"."name" = @p0) AND ("t0"."email" LIKE @p1))
                ORDER BY
                	"t0"."name" DESC
                LIMIT
                	@p2
                """,
                build.Sql);

            AssertParameters(build, "Engineering", "%@t.com", 5);
        }

        [Fact]
        public void ESQJsonModel_ShouldRestoreFromJsonString()
        {
            var json = """
            {
              "tableName": "employees",
              "columns": [
                { "path": "Name" },
                { "path": "Salary" }
              ],
              "filters": {
                "items": [
                  { "path": "Salary", "comparisonType": "GreaterThanOrEqual", "value": 100 }
                ]
              },
              "orders": [
                { "path": "Name" }
              ]
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name", "t0"."salary" AS "Salary"
                FROM
                	"employees" AS "t0"
                WHERE
                	("t0"."salary" >= @p0)
                ORDER BY
                	"t0"."name" ASC
                """,
                build.Sql);

            AssertParameters(build, 100);
        }

        [Fact]
        public void ESQJsonModel_NestedFilterGroup_ShouldRestoreGroupedWhere()
        {
            var json = """
            {
              "tableName": "employees",
              "columns": [
                { "path": "Name" }
              ],
              "filters": {
                "logicalOperation": 0,
                "items": [
                  { "path": "Name", "comparisonType": 8, "value": "A%" },
                  {
                    "logicalOperation": 1,
                    "items": [
                      { "path": "Email", "comparisonType": 8, "value": "%@t.com" },
                      { "path": "Salary", "comparisonType": 3, "value": 1000 }
                    ]
                  }
                ]
              }
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"employees" AS "t0"
                WHERE
                	(("t0"."name" LIKE @p0) AND (("t0"."email" LIKE @p1) OR ("t0"."salary" >= @p2)))
                """,
                build.Sql);

            AssertParameters(build, "A%", "%@t.com", 1000);
        }

        [Fact]
        public void ESQJsonModel_ShouldUseSkipRowForOffset()
        {
            var json = """
            {
              "tableName": "employees",
              "columns": [
                { "path": "Name" }
              ],
              "orders": [
                { "path": "Name" }
              ],
              "rowCount": 2,
              "skipRow": 4
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"employees" AS "t0"
                ORDER BY
                	"t0"."name" ASC
                LIMIT
                	@p0
                OFFSET
                	@p1
                """,
                build.Sql);

            AssertParameters(build, 2, 4);
        }

        [Fact]
        public void ESQJsonModel_AggregationCountStar_ShouldBuildCountStar()
        {
            var json = """
            {
              "tableName": "employees",
              "columns": [
                { "path": "*", "alias": "EmployeeCount", "aggregationType": "Count" }
              ]
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	COUNT(*) AS "EmployeeCount"
                FROM
                	"employees" AS "t0"
                """,
                build.Sql);

            AssertParameters(build);
        }

        [Fact]
        public void ESQJsonModel_AggregationGroupBy_ShouldBuildAggregateColumns()
        {
            var json = """
            {
              "tableName": "employees",
              "columns": [
                { "path": "DepartmentId.Name", "alias": "DepartmentName" },
                { "path": "Id", "alias": "EmployeeCount", "aggregationType": "Count" },
                { "path": "Salary", "alias": "TotalSalary", "aggregationType": "Sum" },
                { "path": "Salary", "alias": "AverageSalary", "aggregationType": "Avg" },
                { "path": "Salary", "alias": "MinSalary", "aggregationType": "Min" },
                { "path": "Salary", "alias": "MaxSalary", "aggregationType": "Max" }
              ],
              "groupBy": [
                "DepartmentId.Name"
              ],
              "orders": [
                { "path": "DepartmentId.Name" }
              ]
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t1"."name" AS "DepartmentName", COUNT("t0"."id") AS "EmployeeCount", SUM("t0"."salary") AS "TotalSalary", AVG("t0"."salary") AS "AverageSalary", MIN("t0"."salary") AS "MinSalary", MAX("t0"."salary") AS "MaxSalary"
                FROM
                	"employees" AS "t0"
                LEFT JOIN
                	"departments" AS "t1"
                ON
                	("t0"."department_id" = "t1"."id")
                GROUP BY
                	"t1"."name"
                ORDER BY
                	"t1"."name" ASC
                """,
                build.Sql);

            AssertParameters(build);
        }

        [Fact]
        public void ESQJsonModel_ShouldRoundTripFromESQ()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(_provider, OrmTestUserConnection.Create());
            esq.AddColumn("Name", "EmployeeName");
            esq.AddFilter(ConditionOperator.Equal, "Email", "ivan@example.com");
            esq.OrderBy("Name");
            esq.RowCount = 1;

            var json = esq.ToJsonModel().ToJson();
            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create()).Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "EmployeeName"
                FROM
                	"employees" AS "t0"
                WHERE
                	("t0"."email" = @p0)
                ORDER BY
                	"t0"."name" ASC
                LIMIT
                	@p1
                """,
                build.Sql);

            AssertParameters(build, "ivan@example.com", 1);
        }

        [Fact]
        public void EntitySelectBuilder_LocalizedColumn_ShouldBuildLczJoinAndFallback()
        {
            var cultureId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var build = EntityManager.Select<OrmLocalizedDepartmentEntity>(_provider, OrmTestUserConnection.Create(cultureId))
                .AddColumn("Name")
                .Build();

            AssertSql(
                """
                SELECT
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") AS "Name"
                FROM
                	"departments" AS "t0"
                LEFT JOIN
                	"sys_departments_lcz" AS "t1"
                ON
                	(("t1"."RecordId" = "t0"."id") AND ("t1"."SysCultureId" = @p0))
                """,
                build.Sql);

            AssertParameters(build, cultureId);
        }

        [Fact]
        public void EntitySelectBuilder_UserConnectionWithoutLocalizedColumn_ShouldNotBuildLczJoin()
        {
            var cultureId = Guid.Parse("12121212-1212-1212-1212-121212121212");

            var build = EntityManager.Select<OrmDepartmentEntity>(_provider, OrmTestUserConnection.Create(cultureId))
                .AddColumn("Name")
                .Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"departments" AS "t0"
                """,
                build.Sql);

            AssertParameters(build);
        }

        [Fact]
        public void EntitySelectBuilder_LocalizedFilterAndOrder_ShouldUseFallbackExpression()
        {
            var cultureId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            var build = EntityManager.Select<OrmLocalizedDepartmentEntity>(_provider, OrmTestUserConnection.Create(cultureId))
                .AddColumn("Name")
                .Where("Name").IsLike("Dev%")
                .OrderBy("Name")
                .Build();

            AssertSql(
                """
                SELECT
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") AS "Name"
                FROM
                	"departments" AS "t0"
                LEFT JOIN
                	"sys_departments_lcz" AS "t1"
                ON
                	(("t1"."RecordId" = "t0"."id") AND ("t1"."SysCultureId" = @p0))
                WHERE
                	(COALESCE(NULLIF("t1"."name", ''), "t0"."name") LIKE @p1)
                ORDER BY
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") ASC
                """,
                build.Sql);

            AssertParameters(build, cultureId, "Dev%");
        }

        [Fact]
        public void ESQJsonModel_LocalizedAggregationGroupBy_ShouldGroupPhysicalColumns()
        {
            var cultureId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var json = """
            {
              "entityTypeName": "OrmLocalizedDepartmentEntity",
              "columns": [
                { "path": "Name" },
                { "path": "Id", "alias": "DepartmentCount", "aggregationType": "Count" }
              ],
              "groupBy": [
                "Name"
              ],
              "orders": [
                { "path": "Name" }
              ]
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create(cultureId)).Build();

            AssertSql(
                """
                SELECT
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") AS "Name", COUNT("t0"."id") AS "DepartmentCount"
                FROM
                	"departments" AS "t0"
                LEFT JOIN
                	"sys_departments_lcz" AS "t1"
                ON
                	(("t1"."RecordId" = "t0"."id") AND ("t1"."SysCultureId" = @p0))
                GROUP BY
                	"t1"."name", "t0"."name"
                ORDER BY
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") ASC
                """,
                build.Sql);

            AssertParameters(build, cultureId);
        }

        [Fact]
        public void ESQJsonModel_Localization_ShouldRestoreLocalizationContext()
        {
            var cultureId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var json = $$"""
            {
              "entityTypeName": "OrmLocalizedDepartmentEntity",
              "columns": [
                { "path": "Name" }
              ],
              "filters": {
                "items": [
                  { "path": "Name", "comparisonType": "Equal", "value": "Development" }
                ]
              },
              "orders": [
                { "path": "Name" }
              ]
            }
            """;

            var build = ESQJsonModel.FromJson(json).ToESQ(_provider, OrmTestUserConnection.Create(cultureId)).Build();

            AssertSql(
                """
                SELECT
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") AS "Name"
                FROM
                	"departments" AS "t0"
                LEFT JOIN
                	"sys_departments_lcz" AS "t1"
                ON
                	(("t1"."RecordId" = "t0"."id") AND ("t1"."SysCultureId" = @p0))
                WHERE
                	(COALESCE(NULLIF("t1"."name", ''), "t0"."name") = @p1)
                ORDER BY
                	COALESCE(NULLIF("t1"."name", ''), "t0"."name") ASC
                """,
                build.Sql);

            AssertParameters(build, cultureId, "Development");
        }

        [Fact]
        public void ESQ_DisableLocalizationAttribute_ShouldReadMainColumnWithoutLczJoin()
        {
            var cultureId = Guid.Parse("44444444-4444-4444-4444-444444444444");

            var esq = EntityManager.Query<OrmLocalizationDisabledDepartmentEntity>(_provider, OrmTestUserConnection.Create(cultureId));
            esq.AddDisplayColumn();

            var build = esq.Build();

            AssertSql(
                """
                SELECT
                	"t0"."name" AS "Name"
                FROM
                	"departments" AS "t0"
                """,
                build.Sql);

            AssertParameters(build);
        }
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
    }
}











