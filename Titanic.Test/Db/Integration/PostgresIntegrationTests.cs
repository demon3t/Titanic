using System.Data;
using Titanic.Db;
using Titanic.Db.Abstractions;

namespace Titanic.Test.Db.Integration
{
    /// <summary>
    /// Integration tests against a real PostgreSQL database.
    /// </summary>
    public class PostgresIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public PostgresIntegrationTests(IntegrationTestFixture fixture)
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

        #region INSERT

        [SkippableFact]
        public void Insert_01_Department_ShouldReturnId()
        {
            var id = InsertDepartment("Eng01");
            Assert.True(id > 0);
        }

        [SkippableFact]
        public void Insert_02_MultipleRows_ShouldInsertAll()
        {
            InsertDepartment("A");
            InsertDepartment("B");
            InsertDepartment("C");

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("departments").As("t")
                .ExecuteScalar<int>();

            Assert.True(count >= 3);
        }

        [SkippableFact]
        public void Insert_03_Employee_WithForeignKey()
        {
            var departmentId = InsertDepartment("D03");
            InsertEmployee("E03", "e03@t.com", departmentId, 50000);

            var name = Db.Select()
                .Column(Column.Name("e", "name"))
                .From("employees").As("e")
                .Where("e", "email").IsEqual(Column.Parameter("e03@t.com"))
                .ExecuteScalar<string>();

            Assert.Equal("E03", name);
        }

        [SkippableFact]
        public void Insert_04_WithReturning()
        {
            var id = Db.Insert("departments")
                .SetColumns("name", "description")
                .Values(Column.Parameter("Ops04"), Column.Parameter((string?)null))
                .Returning("id")
                .ExecuteScalar<int>();

            Assert.True(id > 0);
        }

        [SkippableFact]
        public void Insert_05_SameEmail_ShouldFail()
        {
            var departmentId = InsertDepartment("D05");
            InsertEmployee("dup@t.com", "dup@t.com", departmentId, 100);

            Assert.NotNull(Record.Exception(() => InsertEmployee("dup@t.com", "dup@t.com", departmentId, 200)));
        }

        [SkippableFact]
        public void Insert_06_PrefixAndCount()
        {
            for (var i = 1; i <= 5; i++)
            {
                InsertDepartment("I06_" + i);
            }

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("departments").As("t")
                .Where("name").IsLike("I06_%")
                .ExecuteScalar<int>();

            Assert.Equal(5, count);
        }

        #endregion INSERT

        #region UPDATE

        [SkippableFact]
        public void Update_01_ShouldModifyRows()
        {
            var departmentId = InsertDepartment("UD01");
            InsertEmployee("u01@t.com", "u01@t.com", departmentId, 50000);

            var affected = Db.Update("employees")
                .Table("employees", "e")
                .Set("salary", Column.Parameter(60000.0))
                .Where("e", "email").IsEqual(Column.Parameter("u01@t.com"))
                .Execute();

            Assert.Equal(1, affected);
        }

        [SkippableFact]
        public void Update_02_WithReturning()
        {
            var departmentId = InsertDepartment("UD02");
            InsertEmployee("u02@t.com", "u02@t.com", departmentId, 50000);

            var id = Db.Update("employees")
                .Table("employees", "e")
                .Set("salary", Column.Parameter(70000.0))
                .Where("e", "email").IsEqual(Column.Parameter("u02@t.com"))
                .Returning("id")
                .ExecuteScalar<int>();

            Assert.True(id > 0);
        }

        [SkippableFact]
        public void Update_03_WithAndCondition()
        {
            var departmentId = InsertDepartment("UD03");
            InsertEmployee("u03@t.com", "u03@t.com", departmentId, 50000);

            var affected = Db.Update("employees")
                .Table("employees", "e")
                .Set("salary", Column.Parameter(55000.0))
                .Where("e", "email").IsEqual(Column.Parameter("u03@t.com"))
                .And("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .Execute();

            Assert.Equal(1, affected);
        }

        [SkippableFact]
        public void Update_04_WithOrCondition()
        {
            var departmentId = InsertDepartment("UD04");
            InsertEmployee("u04a@t.com", "u04a@t.com", departmentId, 100);
            InsertEmployee("u04b@t.com", "u04b@t.com", departmentId, 200);

            var affected = Db.Update("employees")
                .Table("employees", "e")
                .Set("salary", Column.Parameter(70000.0))
                .Where("e", "email").IsEqual(Column.Parameter("u04a@t.com"))
                .Or("e", "email").IsEqual(Column.Parameter("u04b@t.com"))
                .Execute();

            Assert.Equal(2, affected);
        }

        [SkippableFact]
        public void Update_05_MultipleRows()
        {
            var departmentId = InsertDepartment("UD05");
            InsertEmployee("u05a@t.com", "u05a@t.com", departmentId, 100);
            InsertEmployee("u05b@t.com", "u05b@t.com", departmentId, 200);

            var affected = Db.Update("employees")
                .Table("employees", "e")
                .Set("salary", Column.Parameter(300.0))
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .Execute();

            Assert.Equal(2, affected);
        }

        [SkippableFact]
        public void Update_06_SetStringColumn()
        {
            var departmentId = InsertDepartment("UD06");
            InsertEmployee("u06@t.com", "u06@t.com", departmentId, 50000);

            var affected = Db.Update("employees")
                .Table("employees", "e")
                .Set("name", Column.Parameter("NewName"))
                .Where("e", "email").IsEqual(Column.Parameter("u06@t.com"))
                .Execute();

            Assert.Equal(1, affected);
        }

        #endregion UPDATE

        #region DELETE

        [SkippableFact]
        public void Delete_01_ShouldRemoveRows()
        {
            var departmentId = InsertDepartment("DD01");
            InsertEmployee("del01@t.com", "del01@t.com", departmentId, 100);

            var affected = Db.Delete("employees")
                .From("employees", "e")
                .Where("e", "email").IsEqual(Column.Parameter("del01@t.com"))
                .Execute();

            Assert.Equal(1, affected);
        }

        [SkippableFact]
        public void Delete_02_WithReturning()
        {
            var departmentId = InsertDepartment("DD02");
            InsertEmployee("del02@t.com", "del02@t.com", departmentId, 100);

            var id = Db.Delete("employees")
                .From("employees", "e")
                .Where("e", "email").IsEqual(Column.Parameter("del02@t.com"))
                .Returning("id")
                .ExecuteScalar<int>();

            Assert.True(id > 0);
        }

        [SkippableFact]
        public void Delete_03_WithAndCondition()
        {
            var departmentId = InsertDepartment("DD03");
            InsertEmployee("del03@t.com", "del03@t.com", departmentId, 100);

            var affected = Db.Delete("employees")
                .From("employees", "e")
                .Where("e", "email").IsEqual(Column.Parameter("del03@t.com"))
                .And("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .Execute();

            Assert.Equal(1, affected);
        }

        [SkippableFact]
        public void Delete_04_NoMatchingRows()
        {
            var affected = Db.Delete("employees")
                .From("employees", "e")
                .Where("e", "email").IsEqual(Column.Parameter("nonexistent_del@t.com"))
                .Execute();

            Assert.Equal(0, affected);
        }

        [SkippableFact]
        public void Delete_05_MultipleRows()
        {
            var departmentId = InsertDepartment("DD05");
            InsertEmployee("d05a@t.com", "d05a@t.com", departmentId, 100);
            InsertEmployee("d05b@t.com", "d05b@t.com", departmentId, 200);

            var affected = Db.Delete("employees")
                .From("employees", "e")
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .Execute();

            Assert.Equal(2, affected);
        }

        [SkippableFact]
        public void Delete_06_WithOrCondition()
        {
            var departmentId = InsertDepartment("DD06");
            InsertEmployee("del06@t.com", "del06@t.com", departmentId, 100);

            var affected = Db.Delete("employees")
                .From("employees", "e")
                .Where("e", "email").IsEqual(Column.Parameter("del06@t.com"))
                .Or("e", "email").IsEqual(Column.Parameter("nonexistent_d6@t.com"))
                .Execute();

            Assert.Equal(1, affected);
        }

        #endregion DELETE

        #region SELECT

        [SkippableFact]
        public void Select_01_InnerJoin()
        {
            var departmentId = InsertDepartment("Eng01");
            InsertEmployee("Bob", "bob01@t.com", departmentId, 80000);
            InsertEmployee("Carol", "carol01@t.com", departmentId, 90000);

            var rows = new List<(string Name, double Salary, string DeptName)>();
            Db.Select()
                .Column(Column.Name("e", "name"))
                .Column(Column.Name("e", "salary"))
                .Column(Column.Name("d", "name").As("dept_name"))
                .From("employees").As("e")
                .InnerJoin("departments").As("d").On("e", "department_id").IsEqual("d", "id")
                .Where("d", "name").IsEqual(Column.Parameter("Eng01"))
                .OrderBy("e", "name")
                .ExecuteReader(r => rows.Add((r.Get<string>("name"), r.Get<double>("salary"), r.Get<string>("dept_name"))));

            Assert.Equal(2, rows.Count);
        }

        [SkippableFact]
        public void Select_02_LeftJoin()
        {
            InsertDepartment("Empty02");

            var rows = new List<(string DeptName, string? EmpName)>();
            Db.Select()
                .Column(Column.Name("d", "name").As("dept_name"))
                .Column(Column.Name("e", "name").As("emp_name"))
                .From("departments").As("d")
                .LeftJoin("employees").As("e").On("d", "id").IsEqual("e", "department_id")
                .Where("d", "name").IsEqual(Column.Parameter("Empty02"))
                .ExecuteReader(r => rows.Add((
                    r.Get<string>("dept_name"),
                    r.IsDBNull(r.GetOrdinal("emp_name")) ? null : r.Get<string>("emp_name"))));

            Assert.Single(rows);
            Assert.Null(rows[0].EmpName);
        }

        [SkippableFact]
        public void Select_03_RightJoin()
        {
            var departmentId = InsertDepartment("Right03");
            InsertEmployee("r03@t.com", "r03@t.com", departmentId, 100);

            var rows = new List<(string? DeptName, string EmpName)>();
            Db.Select()
                .Column(Column.Name("d", "name").As("dept_name"))
                .Column(Column.Name("e", "name").As("emp_name"))
                .From("departments").As("d")
                .RightJoin("employees").As("e").On("d", "id").IsEqual("e", "department_id")
                .Where("e", "email").IsEqual(Column.Parameter("r03@t.com"))
                .ExecuteReader(r => rows.Add((
                    r.IsDBNull(r.GetOrdinal("dept_name")) ? null : r.Get<string>("dept_name"),
                    r.Get<string>("emp_name"))));

            Assert.Single(rows);
        }

        [SkippableFact]
        public void Select_04_OrderBy_Desc()
        {
            InsertDepartment("A04");
            InsertDepartment("B04");
            InsertDepartment("C04");

            var rows = new List<string>();
            Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("%04")
                .OrderBy("name", desc: true)
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Equal("C04", rows[0]);
        }

        [SkippableFact]
        public void Select_05_Limit()
        {
            for (var i = 1; i <= 5; i++)
            {
                InsertDepartment("L05_" + i);
            }

            var rows = new List<string>();
            Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("L05_%")
                .OrderBy("name")
                .Limit(2)
                .Take(2)
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Equal(2, rows.Count);
        }

        [SkippableFact]
        public void Select_06_OrderBy_Asc()
        {
            InsertDepartment("Z06");
            InsertDepartment("A06");

            var rows = new List<string>();
            Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("%06")
                .OrderBy("name")
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Equal("A06", rows[0]);
        }

        [SkippableFact]
        public void Select_07_Count()
        {
            InsertDepartment("D07a");
            InsertDepartment("D07b");

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("departments").As("t")
                .Where("name").IsLike("D07%")
                .ExecuteScalar<int>();

            Assert.Equal(2, count);
        }

        [SkippableFact]
        public void Select_08_Sum()
        {
            var departmentId = InsertDepartment("Sum08");
            InsertEmployee("s08a@t.com", "s08a@t.com", departmentId, 100);
            InsertEmployee("s08b@t.com", "s08b@t.com", departmentId, 200);

            var sum = Db.Select()
                .Column(Func.Sum(Column.Name("e", "salary")))
                .From("employees").As("e")
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .ExecuteScalar<double>();

            Assert.Equal(300, sum);
        }

        [SkippableFact]
        public void Select_09_Avg()
        {
            var departmentId = InsertDepartment("Avg09");
            InsertEmployee("a09a@t.com", "a09a@t.com", departmentId, 100);
            InsertEmployee("a09b@t.com", "a09b@t.com", departmentId, 200);

            var average = Db.Select()
                .Column(Func.Avg(Column.Name("e", "salary")))
                .From("employees").As("e")
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .ExecuteScalar<double>();

            Assert.Equal(150, average);
        }

        [SkippableFact]
        public void Select_10_GroupBy()
        {
            var firstDepartmentId = InsertDepartment("Dev10");
            var secondDepartmentId = InsertDepartment("QA10");
            InsertEmployee("g10a@t.com", "g10a@t.com", firstDepartmentId, 100);
            InsertEmployee("g10b@t.com", "g10b@t.com", firstDepartmentId, 200);
            InsertEmployee("g10c@t.com", "g10c@t.com", secondDepartmentId, 300);

            var rows = new List<(string Dept, int Cnt)>();
            Db.Select()
                .Column(Column.Name("d", "name").As("dept"))
                .Column(Func.Count(Column.Asterisk()).As("cnt"))
                .From("employees").As("e")
                .InnerJoin("departments").As("d").On("e", "department_id").IsEqual("d", "id")
                .GroupBy("d", "name")
                .OrderBy("d", "name")
                .ExecuteReader(r => rows.Add((r.Get<string>("dept"), r.Get<int>("cnt"))));

            Assert.Equal(2, rows.Count);
            Assert.Equal(2, rows[0].Cnt);
        }

        [SkippableFact]
        public void Select_11_Like()
        {
            InsertDepartment("Alpha11");
            InsertDepartment("Alphabet11");
            InsertDepartment("Beta11");

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("departments").As("t")
                .Where("name").IsLike("Alpha%")
                .ExecuteScalar<int>();

            Assert.Equal(2, count);
        }

        [SkippableFact]
        public void Select_12_And()
        {
            var departmentId = InsertDepartment("And12");
            InsertEmployee("a12@t.com", "a12@t.com", departmentId, 100);

            var name = Db.Select()
                .Column(Column.Name("e", "name"))
                .From("employees").As("e")
                .Where("e", "email").IsEqual(Column.Parameter("a12@t.com"))
                .And("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .ExecuteScalar<string>();

            Assert.Equal("a12@t.com", name);
        }

        [SkippableFact]
        public void Select_13_Or()
        {
            var departmentId = InsertDepartment("Or13");
            InsertEmployee("o13a@t.com", "o13a@t.com", departmentId, 100);
            InsertEmployee("o13b@t.com", "o13b@t.com", departmentId, 200);

            var rows = new List<string>();
            Db.Select().Column("email").From("employees").As("e")
                .Where("e", "email").IsEqual(Column.Parameter("o13a@t.com"))
                .Or("e", "email").IsEqual(Column.Parameter("o13b@t.com"))
                .OrderBy("email")
                .ExecuteReader(r => rows.Add(r.Get<string>("email")));

            Assert.Equal(2, rows.Count);
        }

        [SkippableFact]
        public void Select_14_Union()
        {
            InsertDepartment("U14_A");
            InsertDepartment("U14_B");

            var rows = new List<string>();
            var first = Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("U14_%");
            var second = Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("U14_%");

            first.Union(second)
                .OrderBy("name")
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Equal(2, rows.Distinct().Count());
        }

        [SkippableFact]
        public void Select_15_SubQuery_InWhere()
        {
            var departmentId = InsertDepartment("Parent15");
            InsertEmployee("s15@t.com", "s15@t.com", departmentId, 100);

            var subQuery = Db.Select().Column("id").From("departments").As("t")
                .Where("name").IsEqual(Column.Parameter("Parent15"));

            var rows = new List<int>();
            Db.Select().Column(Column.Name("e", "department_id"))
                .From("employees").As("e")
                .Where("e", "department_id").In(subQuery)
                .ExecuteReader(r => rows.Add(r.GetInt32(0)));

            Assert.Single(rows);
        }

        [SkippableFact]
        public void Select_16_SelfJoin()
        {
            var parentId = InsertCategory("Parent16");
            InsertCategory("Child16", parentId);

            var rows = new List<(string Parent, string Child)>();
            Db.Select()
                .Column(Column.Name("p", "name").As("parent"))
                .Column(Column.Name("c", "name").As("child"))
                .From("categories").As("c")
                .InnerJoin("categories").As("p").On("c", "parent_id").IsEqual("p", "id")
                .Where("p", "name").IsEqual(Column.Parameter("Parent16"))
                .ExecuteReader(r => rows.Add((r.Get<string>("parent"), r.Get<string>("child"))));

            Assert.Single(rows);
        }

        [SkippableFact]
        public void Select_17_RowCount()
        {
            var departmentId = InsertDepartment("RC17");
            InsertEmployee("rc17a@t.com", "rc17a@t.com", departmentId, 100);
            InsertEmployee("rc17b@t.com", "rc17b@t.com", departmentId, 200);

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("employees").As("e")
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .ExecuteScalar<int>();

            Assert.Equal(2, count);
        }

        [SkippableFact]
        public void Select_18_ById()
        {
            var departmentId = InsertDepartment("ById18");
            var employeeId = InsertEmployee("byid18@t.com", "byid18@t.com", departmentId, 50000);

            var name = Db.Select()
                .Column(Column.Name("e", "name"))
                .From("employees").As("e")
                .Where("e", "id").IsEqual(Column.Parameter(employeeId))
                .ExecuteScalar<string>();

            Assert.Equal("byid18@t.com", name);
        }

        [SkippableFact]
        public void Select_19_CountByDepartment()
        {
            var departmentId = InsertDepartment("Cnt19");
            InsertEmployee("c19a@t.com", "c19a@t.com", departmentId, 100);
            InsertEmployee("c19b@t.com", "c19b@t.com", departmentId, 200);
            InsertEmployee("c19c@t.com", "c19c@t.com", departmentId, 300);

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("employees").As("e")
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .ExecuteScalar<int>();

            Assert.Equal(3, count);
        }

        [SkippableFact]
        public void Select_20_StringColumn()
        {
            var departmentId = InsertDepartment("Str20");
            InsertEmployee("s20@t.com", "s20@t.com", departmentId, 100);

            var name = Db.Select()
                .Column(Column.Name("e", "name"))
                .From("employees").As("e")
                .Where("e", "email").IsEqual(Column.Parameter("s20@t.com"))
                .ExecuteScalar<string>();

            Assert.Equal("s20@t.com", name);
        }

        [SkippableFact]
        public void Select_21_NumericColumn()
        {
            var departmentId = InsertDepartment("Num21");
            InsertEmployee("n21@t.com", "n21@t.com", departmentId, 75000);

            var salary = Db.Select()
                .Column(Column.Name("e", "salary"))
                .From("employees").As("e")
                .Where("e", "email").IsEqual(Column.Parameter("n21@t.com"))
                .ExecuteScalar<double>();

            Assert.Equal(75000, salary);
        }

        [SkippableFact]
        public void Select_22_AliasFilterAndOrder()
        {
            var departmentId = InsertDepartment("Alias22");
            InsertEmployee("a22a@t.com", "a22a@t.com", departmentId, 100);
            InsertEmployee("a22b@t.com", "a22b@t.com", departmentId, 200);

            var names = new List<string>();
            Db.Select()
                .Column(Column.Name("e", "name"))
                .From("employees").As("e")
                .Where("e", "department_id").IsEqual(Column.Parameter(departmentId))
                .OrderBy("e", "name")
                .ExecuteReader(r => names.Add(r.Get<string>("name")));

            Assert.Equal(2, names.Count);
            Assert.Equal("a22a@t.com", names[0]);
        }

        [SkippableFact]
        public void Select_23_NotNullFilter()
        {
            var departmentId = InsertDepartment("NotNull23");
            InsertEmployee("nn23@t.com", "nn23@t.com", departmentId, 100);

            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("employees").As("e")
                .Where("e", "department_id").IsNotNull()
                .ExecuteScalar<int>();

            Assert.True(count >= 1);
        }

        [SkippableFact]
        public void Select_24_GetInstance_Singleton()
        {
            var first = DbManager.Get<TestDatabase>();
            var second = DbManager.Get<TestDatabase>();

            Assert.Same(first, second);
        }

        [SkippableFact]
        public void Select_25_GetInstance_Name()
        {
            var db = DbManager.Get<TestDatabase>();
            Assert.Equal("test", db.Name);
        }

        [SkippableFact]
        public void Select_26_FluentSelect()
        {
            var count = Db.Select()
                .Column(Func.Count(Column.Asterisk()))
                .From("departments").As("t")
                .ExecuteScalar<int>();

            Assert.True(count >= 0);
        }

        [SkippableFact]
        public void Select_27_Offset_ShouldSkipFirstRow()
        {
            InsertDepartment("Offset27_A");
            InsertDepartment("Offset27_B");
            InsertDepartment("Offset27_C");

            var rows = new List<string>();
            Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("Offset27_%")
                .OrderBy("name")
                .Limit(10)
                .Skip(1)
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Equal(2, rows.Count);
            Assert.Equal("Offset27_B", rows[0]);
        }

        [SkippableFact]
        public void Select_28_Page_ShouldReturnSecondPage()
        {
            for (var i = 1; i <= 5; i++)
            {
                InsertDepartment($"Page28_{i}");
            }

            var rows = new List<string>();
            Db.Select().Column("name").From("departments").As("t")
                .Where("name").IsLike("Page28_%")
                .OrderBy("name")
                .Limit(2)
                .Page(2, 2)
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Equal(2, rows.Count);
            Assert.Equal("Page28_3", rows[0]);
            Assert.Equal("Page28_4", rows[1]);
        }

        [SkippableFact]
        public void Select_29_Having_ShouldFilterGroupedRows()
        {
            var firstDepartmentId = InsertDepartment("Having29_A");
            var secondDepartmentId = InsertDepartment("Having29_B");
            InsertEmployee("h29a1@t.com", "h29a1@t.com", firstDepartmentId, 100);
            InsertEmployee("h29a2@t.com", "h29a2@t.com", firstDepartmentId, 200);
            InsertEmployee("h29b1@t.com", "h29b1@t.com", secondDepartmentId, 300);

            var rows = new List<string>();
            Db.Select()
                .Column(Column.Name("d", "name").As("dept"))
                .Column(Func.Count(Column.Asterisk()).As("cnt"))
                .From("employees").As("e")
                .InnerJoin("departments").As("d").On("e", "department_id").IsEqual("d", "id")
                .GroupBy("d", "name")
                .Having(Func.Count(Column.Asterisk())).IsGreaterThan(1)
                .OrderBy("d", "name")
                .ExecuteReader(r => rows.Add(r.Get<string>("dept")));

            Assert.Single(rows);
            Assert.Equal("Having29_A", rows[0]);
        }

        [SkippableFact]
        public void Select_30_IsNull_ShouldFindDepartmentWithoutDescription()
        {
            InsertDepartment("Null30_WithDescription", "has value");
            InsertDepartment("Null30_WithoutDescription");

            var rows = new List<string>();
            Db.Select()
                .Column("name")
                .From("departments").As("t")
                .Where("description").IsNull()
                .OrderBy("name")
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Contains("Null30_WithoutDescription", rows);
            Assert.DoesNotContain("Null30_WithDescription", rows);
        }

        [SkippableFact]
        public void Select_31_NotLike_ShouldExcludeMatchingRows()
        {
            InsertDepartment("Bad31_A");
            InsertDepartment("Bad31_B");
            InsertDepartment("Good31_A");

            var rows = new List<string>();
            Db.Select()
                .Column("name")
                .From("departments").As("t")
                .Where("name").Not().IsLike("Bad31_%")
                .Where("name").IsLike("%31_%")
                .OrderBy("name")
                .ExecuteReader(r => rows.Add(r.Get<string>("name")));

            Assert.Single(rows);
            Assert.Equal("Good31_A", rows[0]);
        }

        [SkippableFact]
        public void Select_32_Between_ShouldFilterSalaryRange()
        {
            var departmentId = InsertDepartment("Between32");
            InsertEmployee("b32a@t.com", "b32a@t.com", departmentId, 100);
            InsertEmployee("b32b@t.com", "b32b@t.com", departmentId, 250);
            InsertEmployee("b32c@t.com", "b32c@t.com", departmentId, 400);

            var rows = new List<string>();
            Db.Select()
                .Column(Column.Name("e", "email"))
                .From("employees").As("e")
                .Where("e", "salary").Between(200.0, 300.0)
                .OrderBy("e", "email")
                .ExecuteReader(r => rows.Add(r.Get<string>("email")));

            Assert.Single(rows);
            Assert.Equal("b32b@t.com", rows[0]);
        }

        [SkippableFact]
        public void Select_33_ExecuteReaderList_ShouldReturnMappedRows()
        {
            InsertDepartment("Reader33_A");
            InsertDepartment("Reader33_B");

            var rows = Db.Select()
                .Column("name")
                .From("departments").As("t")
                .Where("name").IsLike("Reader33_%")
                .OrderBy("name")
                .ExecuteReader(r => r.Get<string>("name"));

            Assert.Equal(2, rows.Count);
            Assert.Equal("Reader33_A", rows[0]);
            Assert.Equal("Reader33_B", rows[1]);
        }

        [SkippableFact]
        public void Select_34_ExecuteReaderList_IDataReader_ShouldReturnMappedRows()
        {
            InsertDepartment("Reader34_A");
            InsertDepartment("Reader34_B");

            var rows = Db.Select()
                .Column("name")
                .From("departments").As("t")
                .Where("name").IsLike("Reader34_%")
                .OrderBy("name")
                .ExecuteReader((IDataReader r) => r.GetString(r.GetOrdinal("name")));

            Assert.Equal(2, rows.Count);
            Assert.Equal("Reader34_A", rows[0]);
            Assert.Equal("Reader34_B", rows[1]);
        }

        [SkippableFact]
        public void Select_35_DatabaseExecuteReaderList_FromQuery_ShouldReturnMappedRows()
        {
            InsertDepartment("Reader35_A");
            InsertDepartment("Reader35_B");

            var query = Db.Select()
                .Column("name")
                .From("departments").As("t")
                .Where("name").IsLike("Reader35_%")
                .OrderBy("name");

            var rows = Db.ExecuteReader(query, r => r.Get<string>("name"));

            Assert.Equal(2, rows.Count);
            Assert.Equal("Reader35_A", rows[0]);
            Assert.Equal("Reader35_B", rows[1]);
        }

        #endregion SELECT

        #region Helpers

        private static int InsertDepartment(string name, string? description = null)
        {
            return Db.Insert("departments")
                .SetColumns("name", "description")
                .Values(Column.Parameter(name), Column.Parameter(description))
                .Returning("id")
                .ExecuteScalar<int>();
        }

        private static int InsertEmployee(string name, string email, int departmentId, double salary)
        {
            return Db.Insert("employees")
                .SetColumns("name", "email", "department_id", "salary")
                .Values(
                    Column.Parameter(name),
                    Column.Parameter(email),
                    Column.Parameter(departmentId),
                    Column.Parameter(salary))
                .Returning("id")
                .ExecuteScalar<int>();
        }

        private static int InsertCategory(string name, int? parentId = null)
        {
            return Db.Insert("categories")
                .SetColumns("name", "parent_id")
                .Values(Column.Parameter(name), Column.Parameter(parentId))
                .Returning("id")
                .ExecuteScalar<int>();
        }

        #endregion Helpers
    }
}




