using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity;
using EntityManager = Titanic.Entity.EntityManager;
using Titanic.Entity.Orm;
using Titanic.Test.Db.Integration;
using Orm = Titanic.Entity.Orm;

namespace Titanic.Test.Entity
{
    public class EntityOrmIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public EntityOrmIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;

            Exception exception = null!;
            if (!DbManager.Get<TestDatabase>().CheckConnection(ref exception))
            {
                Skip.IfNot(false,
                    $"PostgreSQL is not available, skipping integration test. Reason: {exception?.Message}");
            }

            _fixture.TruncateAll();
        }

        private static TestDatabase Db => DbManager.Get<TestDatabase>();

        private static Titanic.Common.Session.UserConnection UserConnection => OrmTestUserConnection.Create();

        [SkippableFact]
        public void EntityOrm_01_SaveDepartment_ShouldInsert()
        {
            var department = CreateDepartment("EO01");

            Assert.False(department.IsNew);
            Assert.True(department.Get<int>("Id") > 0);
        }

        [SkippableFact]
        public void EntityOrm_02_SaveDepartment_WithIndexer_ShouldInsert()
        {
            var department = EntityManager.Create<OrmDepartmentEntity>(Db, UserConnection);
            department["Name"] = "EO02";

            Assert.True(department.Save());
            Assert.Equal("EO02", QueryDepartments("EO02").Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_03_CreateByType_ShouldInsert()
        {
            var department = EntityManager.Create(typeof(OrmDepartmentEntity), Db, UserConnection)
                .Set("Name", "EO03");

            Assert.True(department.Save());
            Assert.Equal("EO03", QueryDepartments("EO03").Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_04_CreateByTableName_ShouldInsert()
        {
            var department = EntityManager.Create("departments", Db, UserConnection)
                .Set("Name", "EO04");

            Assert.True(department.Save());
            Assert.Equal("EO04", QueryDepartments("EO04").Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_05_Save_ShouldSetPrimaryKey()
        {
            var department = CreateDepartment("EO05");

            Assert.True(department.Contains("Id"));
            Assert.True(department.Get<int>("id") > 0);
        }

        [SkippableFact]
        public void EntityOrm_06_SaveExistingDepartment_ShouldUpdate()
        {
            var department = CreateDepartment("EO06", "before");
            department.Set("Description", "after");

            Assert.True(department.Save());
            Assert.Equal("after", QueryDepartments("EO06").Single().Get<string>("Description"));
        }

        [SkippableFact]
        public void EntityOrm_07_SaveExistingEmployee_ShouldUpdate()
        {
            var employee = CreateEmployee("EO07", "eo07@t.com", CreateDepartment("D07").Get<int>("Id"));
            employee.Set("Name", "EO07_Updated");

            Assert.True(employee.Save());
            Assert.Equal("EO07_Updated", QueryEmployeesByEmail("eo07@t.com").Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_08_SaveWithExplicitPrimaryKey_ShouldInsert()
        {
            var department = EntityManager.Create<OrmDepartmentEntity>(Db, UserConnection)
                .Set("Id", 50008)
                .Set("Name", "EO08");

            Assert.True(department.Save());
            Assert.Equal(50008, QueryDepartments("EO08").Single().Get<int>("Id"));
        }

        [SkippableFact]
        public void EntityOrm_09_DeleteDepartment_ShouldRemove()
        {
            var department = CreateDepartment("EO09");

            Assert.True(department.Delete());
            Assert.Empty(QueryDepartments("EO09"));
        }

        [SkippableFact]
        public void EntityOrm_10_DeleteEmployee_ShouldRemove()
        {
            var employee = CreateEmployee("EO10", "eo10@t.com", CreateDepartment("D10").Get<int>("Id"));

            Assert.True(employee.Delete());
            Assert.Empty(QueryEmployeesByEmail("eo10@t.com"));
        }

        [SkippableFact]
        public void EntityOrm_11_DeleteWithoutPrimaryKey_ShouldThrow()
        {
            var department = EntityManager.Create<OrmDepartmentEntity>(Db, UserConnection).Set("Name", "EO11");

            Assert.Throws<InvalidOperationException>(() => department.Delete());
        }

        [SkippableFact]
        public void EntityOrm_12_Contains_ShouldCheckSelectedPath()
        {
            CreateDepartment("EO12");

            var department = QueryDepartments("EO12").Single();

            Assert.True(department.Contains("Name"));
            Assert.False(department.Contains("Missing"));
        }

        [SkippableFact]
        public void EntityOrm_13_GetByColumnName_ShouldUseColumnAliasMap()
        {
            var department = EntityManager.Create<OrmDepartmentEntity>(Db, UserConnection)
                .Set("name", "EO13");

            Assert.True(department.Save());
            Assert.Equal("EO13", department.Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_14_TryGet_ShouldReturnTypedValue()
        {
            var department = CreateDepartment("EO14");

            Assert.True(department.TryGet<int>("Id", out var id));
            Assert.True(id > 0);
        }

        [SkippableFact]
        public void EntityOrm_15_TryGetMissing_ShouldReturnFalse()
        {
            var department = CreateDepartment("EO15");

            Assert.False(department.TryGet<string>("Missing", out var value));
            Assert.Null(value);
        }

        [SkippableFact]
        public void EntityOrm_16_SetValues_ShouldSetMultipleColumns()
        {
            var department = EntityManager.Create<OrmDepartmentEntity>(Db, UserConnection)
                .SetValues(new Dictionary<string, object?>
                {
                    ["Name"] = "EO16",
                    ["Description"] = "bulk"
                });

            Assert.True(department.Save());
            Assert.Equal("bulk", QueryDepartments("EO16").Single().Get<string>("Description"));
        }

        [SkippableFact]
        public void EntityOrm_17_ToDictionary_ShouldReturnCopy()
        {
            var department = CreateDepartment("EO17");

            var values = department.ToDictionary();
            values["Name"] = "Changed";

            Assert.Equal("EO17", department.Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_18_ESQ_AddPrimaryColumn_ShouldReadId()
        {
            CreateDepartment("EO18");

            var row = QueryDepartments("EO18").Single();

            Assert.True(row.Get<int>("Id") > 0);
        }

        [SkippableFact]
        public void EntityOrm_19_ESQ_AddDisplayColumn_ShouldReadName()
        {
            CreateDepartment("EO19");

            Assert.Equal("EO19", QueryDepartments("EO19").Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_20_ESQ_AddAllSchemaColumns_ShouldReadDescription()
        {
            CreateDepartment("EO20", "all");

            var esq = EntityManager.Query<OrmDepartmentEntity>(Db, UserConnection);
            esq.AddAllSchemaColumns();
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO20");

            Assert.Equal("all", esq.GetEntityCollection().Single().Get<string>("Description"));
        }

        [SkippableFact]
        public void EntityOrm_21_ESQ_EqualFilter_ShouldMatch()
        {
            CreateDepartment("EO21");

            Assert.Single(QueryDepartments("EO21"));
        }

        [SkippableFact]
        public void EntityOrm_22_ESQ_ContainsFilter_ShouldMatch()
        {
            CreateDepartment("EO22_A");
            CreateDepartment("EO22_B");

            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.Contains, "Name", "EO22_");

            Assert.Equal(2, esq.GetEntityCollection().Count);
        }

        [SkippableFact]
        public void EntityOrm_23_ESQ_NotContainsFilter_ShouldExclude()
        {
            CreateDepartment("EO23_Bad");
            CreateDepartment("EO23_Good");

            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.Contains, "Name", "Bad").Not();

            Assert.Equal("EO23_Good", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_23A_ESQ_StartsWithFilter_ShouldMatch()
        {
            CreateDepartment("EO23A_First");
            CreateDepartment("XX23A_Second");

            var esq = DepartmentQuery();
            esq.AddStartsWithFilter("Name", "EO23A_");

            Assert.Equal("EO23A_First", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_23B_ESQ_EndsWithFilter_ShouldMatch()
        {
            CreateDepartment("EO23B_Target");
            CreateDepartment("EO23B_Ignore");

            var esq = DepartmentQuery();
            esq.AddEndsWithFilter("Name", "_Target");

            Assert.Equal("EO23B_Target", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_24_ESQ_NotEqualFilter_ShouldExclude()
        {
            CreateDepartment("EO24_A");
            CreateDepartment("EO24_B");

            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.NotEqual, "Name", "EO24_A");

            Assert.Equal("EO24_B", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_25_ESQ_GreaterThanFilter_ShouldMatch()
        {
            var employee = CreateEmployee("EO25", "eo25@t.com", CreateDepartment("D25").Get<int>("Id"), salary: 250);

            var rows = EmployeeSalaryQuery(ConditionOperator.GreaterThan, 200m);

            Assert.Contains(rows, x => x.Get<int>("Id") == employee.Get<int>("Id"));
        }

        [SkippableFact]
        public void EntityOrm_26_ESQ_GreaterOrEqualFilter_ShouldMatch()
        {
            CreateEmployee("EO26", "eo26@t.com", CreateDepartment("D26").Get<int>("Id"), salary: 260);

            Assert.Single(EmployeeSalaryQuery(ConditionOperator.GreaterThanOrEqual, 260m));
        }

        [SkippableFact]
        public void EntityOrm_27_ESQ_LessThanFilter_ShouldMatch()
        {
            CreateEmployee("EO27", "eo27@t.com", CreateDepartment("D27").Get<int>("Id"), salary: 270);

            Assert.Single(EmployeeSalaryQuery(ConditionOperator.LessThan, 300m));
        }

        [SkippableFact]
        public void EntityOrm_28_ESQ_LessOrEqualFilter_ShouldMatch()
        {
            CreateEmployee("EO28", "eo28@t.com", CreateDepartment("D28").Get<int>("Id"), salary: 280);

            Assert.Single(EmployeeSalaryQuery(ConditionOperator.LessThanOrEqual, 280m));
        }

        [SkippableFact]
        public void EntityOrm_29_ESQ_BetweenFilter_ShouldMatch()
        {
            CreateEmployee("EO29_A", "eo29a@t.com", CreateDepartment("D29").Get<int>("Id"), salary: 100);
            CreateEmployee("EO29_B", "eo29b@t.com", CreateDepartment("D29_B").Get<int>("Id"), salary: 250);

            var esq = EmployeeQuery();
            esq.AddBetweenFilter("Salary", 200m, 300m);

            Assert.Equal("EO29_B", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_30_ESQ_IsNullFilter_ShouldMatch()
        {
            CreateDepartment("EO30_Null");
            CreateDepartment("EO30_NotNull", "value");

            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.Contains, "Name", "EO30_");
            esq.AddIsNullFilter("Description");

            Assert.Equal("EO30_Null", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_31_ESQ_IsNotNullFilter_ShouldMatch()
        {
            CreateDepartment("EO31_Null");
            CreateDepartment("EO31_NotNull", "value");

            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.Contains, "Name", "EO31_");
            esq.AddIsNotNullFilter("Description");

            Assert.Equal("EO31_NotNull", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_32_ESQ_OrFilters_ShouldMatchBoth()
        {
            CreateDepartment("EO32_A");
            CreateDepartment("EO32_B");
            CreateDepartment("EO32_C");

            var esq = DepartmentQuery();
            esq.Filters.LogicalOperation = EntityLogicalOperation.Or;
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO32_A");
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO32_B");

            Assert.Equal(2, esq.GetEntityCollection().Count);
        }

        [SkippableFact]
        public void EntityOrm_33_ESQ_DisabledFilters_ShouldReturnAllSelectedRows()
        {
            CreateDepartment("EO33_A");
            CreateDepartment("EO33_B");

            var esq = DepartmentQuery();
            esq.Filters.IsEnabled = false;
            esq.AddFilter(ConditionOperator.Equal, "Name", "NoMatch");

            Assert.Equal(2, esq.GetEntityCollection().Count);
        }

        [SkippableFact]
        public void EntityOrm_34_ESQ_Distinct_ShouldReturnDistinctRows()
        {
            CreateDepartment("EO34_A");
            CreateDepartment("EO34_B");

            var esq = EntityManager.Query<OrmDepartmentEntity>(Db, UserConnection);
            esq.IsDistinct = true;
            esq.AddDisplayColumn();

            Assert.Equal(2, esq.GetEntityCollection().Select(x => x.Get<string>("Name")).Distinct().Count());
        }

        [SkippableFact]
        public void EntityOrm_35_ESQ_RowCount_ShouldLimitRows()
        {
            CreateDepartment("EO35_A");
            CreateDepartment("EO35_B");

            var esq = DepartmentQuery();
            esq.RowCount = 1;

            Assert.Single(esq.GetEntityCollection());
        }

        [SkippableFact]
        public void EntityOrm_36_ESQ_SkipRowCount_ShouldSkipRows()
        {
            CreateDepartment("EO36_A");
            CreateDepartment("EO36_B");

            var esq = DepartmentQuery();
            esq.OrderBy("Name");
            esq.SkipRowCount = 1;

            Assert.Equal("EO36_B", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_37_ESQ_RowCountAndSkip_ShouldPageRows()
        {
            CreateDepartment("EO37_A");
            CreateDepartment("EO37_B");
            CreateDepartment("EO37_C");

            var esq = DepartmentQuery();
            esq.OrderBy("Name");
            esq.SkipRowCount = 1;
            esq.RowCount = 1;

            Assert.Equal("EO37_B", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_38_ESQ_OrderByAscending_ShouldSortRows()
        {
            CreateDepartment("EO38_B");
            CreateDepartment("EO38_A");

            var esq = DepartmentQuery();
            esq.OrderBy("Name");

            Assert.Equal("EO38_A", esq.GetEntityCollection().First().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_39_ESQ_OrderByDescending_ShouldSortRows()
        {
            CreateDepartment("EO39_A");
            CreateDepartment("EO39_B");

            var esq = DepartmentQuery();
            esq.OrderBy("Name", desc: true);

            Assert.Equal("EO39_B", esq.GetEntityCollection().First().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_40_ESQ_ThenBy_ShouldAddSecondSort()
        {
            var department = CreateDepartment("EO40");
            CreateEmployee("EO40_B", "b@eo40.com", department.Get<int>("Id"));
            CreateEmployee("EO40_A", "a@eo40.com", department.Get<int>("Id"));

            var esq = EmployeeQuery();
            esq.OrderBy("DepartmentId.Name").ThenBy("Name");

            Assert.Equal("EO40_A", esq.GetEntityCollection().First().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_41_ESQ_ByType_ShouldQuery()
        {
            CreateDepartment("EO41");

            var esq = EntityManager.Query(typeof(OrmDepartmentEntity), Db, UserConnection);
            esq.AddDisplayColumn();
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO41");

            Assert.Single(esq.GetEntityCollection());
        }

        [SkippableFact]
        public void EntityOrm_42_ESQ_ByTableName_ShouldQuery()
        {
            CreateDepartment("EO42");

            var esq = EntityManager.Query("departments", Db, UserConnection);
            esq.AddDisplayColumn();
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO42");

            Assert.Equal("EO42", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_43_ESQ_ByAbstractType_ShouldQuery()
        {
            CreateEmployee("EO43", "eo43@t.com", CreateDepartment("D43").Get<int>("Id"));

            var esq = EntityManager.Query(typeof(OrmAbstractEmployeeEntity), Db, UserConnection);
            esq.AddDisplayColumn();
            esq.AddFilter(ConditionOperator.Equal, "Email", "eo43@t.com");

            Assert.Equal("EO43", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_44_ESQ_LeftJoinPath_ShouldReadRelatedColumn()
        {
            var department = CreateDepartment("EO44_Department");
            CreateEmployee("EO44", "eo44@t.com", department.Get<int>("Id"));

            var row = EmployeeWithDepartmentQuery("eo44@t.com").Single();

            Assert.Equal("EO44_Department", row.Get<string>("DepartmentName"));
        }

        [SkippableFact]
        public void EntityOrm_45_ESQ_LeftJoinPathFilter_ShouldFilterByRelatedColumn()
        {
            var department = CreateDepartment("EO45_Department");
            CreateEmployee("EO45", "eo45@t.com", department.Get<int>("Id"));

            var esq = EmployeeQuery();
            esq.AddColumn("DepartmentId.Name", "DepartmentName");
            esq.AddFilter(ConditionOperator.Equal, "DepartmentId.Name", "EO45_Department");

            Assert.Equal("EO45", esq.GetEntityCollection().Single().Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_46_ESQ_RightJoinPath_ShouldReadReverseColumn()
        {
            var employee = CreateEmployee("EO46", "eo46@t.com", CreateDepartment("D46").Get<int>("Id"));
            CreateAddress(employee.Get<int>("Id"), "Moscow", "Street46");

            var row = EmployeeWithAddressQuery("Moscow").Single();

            Assert.Equal("Moscow", row.Get<string>("City"));
        }

        [SkippableFact]
        public void EntityOrm_47_ESQ_RightJoinPathFilter_ShouldFilterByReverseColumn()
        {
            var employee = CreateEmployee("EO47", "eo47@t.com", CreateDepartment("D47").Get<int>("Id"));
            CreateAddress(employee.Get<int>("Id"), "Kazan", "Street47");

            var row = EmployeeWithAddressQuery("Kazan").Single();

            Assert.Equal("EO47", row.Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_48_EntitySelectBuilder_ToRecords_ShouldMaterializeEntities()
        {
            CreateDepartment("EO48");

            var records = EntityManager.Select<OrmDepartmentEntity>(Db, UserConnection)
                .AddColumn("Id")
                .AddColumn("Name")
                .Where("Name").IsEqual("EO48")
                .ToRecords();

            Assert.Single(records);
        }

        [SkippableFact]
        public void EntityOrm_49_EntitySelectBuilder_FirstOrDefaultRecord_ShouldReturnEntity()
        {
            CreateDepartment("EO49");

            var record = EntityManager.Select<OrmDepartmentEntity>(Db, UserConnection)
                .AddColumn("Name")
                .Where("Name").IsEqual("EO49")
                .FirstOrDefaultRecord();

            Assert.NotNull(record);
            Assert.Equal("EO49", record!.Get<string>("Name"));
        }

        [SkippableFact]
        public void EntityOrm_50_ESQ_ExecuteReader_ShouldMapRows()
        {
            CreateDepartment("EO50");

            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO50");

            var rows = esq.ExecuteReader(r => r.Get<string>("Name"));

            Assert.Equal("EO50", rows.Single());
        }

        [SkippableFact]
        public void EntityOrm_51_SaveEmployee_ShouldPersistBooleanColumn()
        {
            CreateEmployee("EO51", "eo51@t.com", CreateDepartment("D51").Get<int>("Id"), active: false);

            var row = QueryEmployeesByEmail("eo51@t.com").Single();

            Assert.False(row.Get<bool>("IsActive"));
        }

        [SkippableFact]
        public void EntityOrm_52_SaveAddress_ShouldPersistBooleanColumn()
        {
            var employee = CreateEmployee("EO52", "eo52@t.com", CreateDepartment("D52").Get<int>("Id"));
            CreateAddress(employee.Get<int>("Id"), "City52", "Street52", isPrimary: false);

            var esq = EntityManager.Query<OrmAddressEntity>(Db, UserConnection);
            esq.AddColumn("IsPrimary");
            esq.AddFilter(ConditionOperator.Equal, "City", "City52");

            Assert.False(esq.GetEntityCollection().Single().Get<bool>("IsPrimary"));
        }

        [SkippableFact]
        public void EntityOrm_53_ESQJsonModel_ShouldRestoreAndExecuteFrontendRequest()
        {
            CreateDepartment("EO53", "from json");

            var json = """
            {
              "tableName": "departments",
              "columns": [
                { "path": "Name" },
                { "path": "Description" }
              ],
              "filters": {
                "items": [
                  { "path": "Name", "comparisonType": "Equal", "value": "EO53" }
                ]
              }
            }
            """;

            var rows = ESQJsonModel.FromJson(json)
                .ToESQ(Db, UserConnection)
                .GetEntityCollection();

            Assert.Single(rows);
            Assert.Equal("from json", rows[0].Get<string>("Description"));
        }

        [SkippableFact]
        public void EntityOrm_54_ReferenceColumn_ShouldReadDisplayValue()
        {
            var department = CreateDepartment("EO54_Department");
            CreateEmployee("EO54", "eo54@t.com", department.Get<int>("Id"));

            var esq = EmployeeQuery();
            esq.AddColumn("DepartmentId");
            esq.AddFilter(ConditionOperator.Equal, "Email", "eo54@t.com");

            var row = esq.GetEntityCollection().Single();
            var value = Assert.IsType<ReferenceColumnValue>(row.Values["DepartmentId"]);

            Assert.Equal(department.Get<int>("Id"), row.Get<int>("DepartmentId"));
            Assert.Equal("EO54_Department", row.GetDisplayValue<string>("DepartmentId"));
            Assert.Equal("EO54_Department", value.DisplayValue);
        }

        [SkippableFact]
        public void EntityOrm_55_LocalizedDepartment_ShouldReadLocalizedValue()
        {
            var cultureId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var department = CreateDepartment("EO55_Base", "base description");

            InsertDepartmentLocalization(
                department.Get<int>("Id"),
                cultureId,
                "EO55_Localized",
                "localized description");

            var esq = EntityManager.Query<OrmLocalizedDepartmentEntity>(Db, OrmTestUserConnection.Create(cultureId));
            esq.AddDisplayColumn();
            esq.AddColumn("Description");
            esq.AddFilter(ConditionOperator.Equal, "Id", department.Get<int>("Id"));

            var row = esq.GetEntityCollection().Single();

            Assert.Equal("EO55_Localized", row.Get<string>("Name"));
            Assert.Equal("localized description", row.Get<string>("Description"));
        }

        [SkippableFact]
        public void EntityOrm_56_LocalizedDepartment_ShouldFallbackToBaseValue()
        {
            var cultureId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var department = CreateDepartment("EO56_Base", "base description");

            var esq = EntityManager.Query<OrmLocalizedDepartmentEntity>(Db, OrmTestUserConnection.Create(cultureId));
            esq.AddDisplayColumn();
            esq.AddColumn("Description");
            esq.AddFilter(ConditionOperator.Equal, "Id", department.Get<int>("Id"));

            var row = esq.GetEntityCollection().Single();

            Assert.Equal("EO56_Base", row.Get<string>("Name"));
            Assert.Equal("base description", row.Get<string>("Description"));
        }

        [SkippableFact]
        public void EntityOrm_57_ESQ_EmptyColumns_ShouldReadAllColumns()
        {
            CreateDepartment("EO57", "all columns");

            var esq = EntityManager.Query<OrmDepartmentEntity>(Db, UserConnection);
            esq.AddFilter(ConditionOperator.Equal, "Name", "EO57");

            var row = esq.GetEntityCollection().Single();

            Assert.True(row.Get<int>("Id") > 0);
            Assert.Equal("EO57", row.Get<string>("Name"));
            Assert.Equal("all columns", row.Get<string>("Description"));
        }

        private static EntitySchemaQuery DepartmentQuery()
        {
            var esq = EntityManager.Query<OrmDepartmentEntity>(Db, UserConnection);
            esq.AddPrimaryColumn();
            esq.AddDisplayColumn();
            esq.AddColumn("Description");
            return esq;
        }

        private static EntitySchemaQuery EmployeeQuery()
        {
            var esq = EntityManager.Query<OrmEmployeeEntity>(Db, UserConnection);
            esq.AddPrimaryColumn();
            esq.AddDisplayColumn();
            esq.AddColumn("Email");
            esq.AddColumn("Salary");
            esq.AddColumn("IsActive");
            return esq;
        }

        private static List<Orm.Entity> QueryDepartments(string name)
        {
            var esq = DepartmentQuery();
            esq.AddFilter(ConditionOperator.Equal, "Name", name);
            return esq.GetEntityCollection();
        }

        private static List<Orm.Entity> QueryEmployeesByEmail(string email)
        {
            var esq = EmployeeQuery();
            esq.AddFilter(ConditionOperator.Equal, "Email", email);
            return esq.GetEntityCollection();
        }

        private static List<Orm.Entity> EmployeeSalaryQuery(ConditionOperator op, decimal value)
        {
            var esq = EmployeeQuery();
            esq.AddFilter(op, "Salary", value);
            return esq.GetEntityCollection();
        }

        private static List<Orm.Entity> EmployeeWithDepartmentQuery(string email)
        {
            var esq = EmployeeQuery();
            esq.AddColumn("DepartmentId.Name", "DepartmentName");
            esq.AddFilter(ConditionOperator.Equal, "Email", email);
            return esq.GetEntityCollection();
        }

        private static List<Orm.Entity> EmployeeWithAddressQuery(string city)
        {
            var esq = EmployeeQuery();
            esq.AddColumn("[EmployeeId:Id:Id].City", "City");
            esq.AddFilter(ConditionOperator.Equal, "[EmployeeId:Id:Id].City", city);
            return esq.GetEntityCollection();
        }

        private static Orm.Entity CreateDepartment(string name, string? description = null)
        {
            var entity = EntityManager.Create<OrmDepartmentEntity>(Db, UserConnection)
                .Set("Name", name)
                .Set("Description", description);

            entity.Save();
            return entity;
        }

        private static Orm.Entity CreateEmployee(
            string name,
            string email,
            int departmentId,
            decimal salary = 100m,
            bool active = true)
        {
            var entity = EntityManager.Create<OrmEmployeeEntity>(Db, UserConnection)
                .Set("Name", name)
                .Set("Email", email)
                .Set("DepartmentId", departmentId)
                .Set("Salary", salary)
                .Set("IsActive", active);

            entity.Save();
            return entity;
        }

        private static Orm.Entity CreateAddress(
            int employeeId,
            string city,
            string street,
            bool isPrimary = true)
        {
            var entity = EntityManager.Create<OrmAddressEntity>(Db, UserConnection)
                .Set("EmployeeId", employeeId)
                .Set("City", city)
                .Set("Street", street)
                .Set("IsPrimary", isPrimary);

            entity.Save();
            return entity;
        }

        private static void InsertDepartmentLocalization(
            int recordId,
            Guid cultureId,
            string name,
            string description)
        {
            Db.Insert("sys_departments_lcz")
                .SetColumns("RecordId", "SysCultureId", "name", "description")
                .Values(
                    Column.Parameter(recordId),
                    Column.Parameter(cultureId),
                    Column.Parameter(name),
                    Column.Parameter(description))
                .Execute();
        }
    }

}







