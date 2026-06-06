using Npgsql;
using Titanic.Db;

namespace Titanic.Test.Db.Integration
{
    /// <summary>
    /// xUnit-фикстура для интеграционных тестов.
    /// Инициализирует <see cref="DbManager"/>, регистрирует <see cref="TestDatabase"/>,
    /// создаёт тестовую БД и набор таблиц.
    /// </summary>
    public sealed class IntegrationTestFixture
    {
        private readonly string _connectionString;

        /// <summary>
        /// Имя провайдера тестовой БД в <see cref="DbManager"/>.
        /// </summary>
        public const string ProviderName = "TestPostgres";

        public IntegrationTestFixture()
        {
            var config = TestConfigurationLoader.LoadDbConfig();
            DbManager.Initialize(config);

            // Явная регистрация обёртки. Имя берётся из [DatabaseConnection] атрибута.
            DbManager.RegisterDatabase<TestDatabase>(ProviderName);

            _connectionString = config.Providers
                .FirstOrDefault(provider => string.Equals(provider.Name, ProviderName, StringComparison.OrdinalIgnoreCase))
                ?.ConnectionString ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                return;
            }

            EnsureDatabaseCreated(_connectionString);
            EnsureTablesCreated();
        }

        /// <summary>
        /// Очистить все таблицы в правильном порядке из-за внешних ключей.
        /// Вызывается в начале каждого теста.
        /// </summary>
        public void TruncateAll()
        {
            var db = DbManager.GetProvider();
            db.Execute("TRUNCATE TABLE sys_departments_lcz RESTART IDENTITY CASCADE");
            db.Table("public.audit_logs").Truncate(cascade: true);
            db.Table("public.product_tags").Truncate(cascade: true);
            db.Table("public.tags").Truncate(cascade: true);
            db.Table("public.order_items").Truncate(cascade: true);
            db.Table("public.orders").Truncate(cascade: true);
            db.Table("public.employee_projects").Truncate(cascade: true);
            db.Table("public.projects").Truncate(cascade: true);
            db.Table("public.addresses").Truncate(cascade: true);
            db.Table("public.products").Truncate(cascade: true);
            db.Table("public.categories").Truncate(cascade: true);
            db.Table("public.employees").Truncate(cascade: true);
            db.Table("public.departments").Truncate(cascade: true);
        }

        private static void EnsureDatabaseCreated(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres"
            };

            using var connection = new NpgsqlConnection(builder.ConnectionString);
            connection.Open();

            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'titanic_test_db'";
            var exists = checkCmd.ExecuteScalar();

            if (exists == null || exists == DBNull.Value)
            {
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = "CREATE DATABASE \"titanic_test_db\"";
                createCmd.ExecuteNonQuery();
            }
        }

        private static void EnsureTablesCreated()
        {
            var db = DbManager.GetProvider();

            // 1. departments
            db.Table("public.departments").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("description", Table.ColumnType.Text)
            );

            db.Table("sys_departments_lcz").CreateIfNotExists(
                [
                    new Table.ColumnDefinition("RecordId", Table.ColumnType.Integer, notNull: true, references: "public.departments(id) ON DELETE CASCADE"),
                    new Table.ColumnDefinition("SysCultureId", Table.ColumnType.Uuid, notNull: true),
                    new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 255),
                    new Table.ColumnDefinition("description", Table.ColumnType.Text)
                ],
                Table.TableConstraint.NamedPrimaryKey("PK_sys_departments_lcz", "RecordId", "SysCultureId"));

            // 2. employees
            db.Table("public.employees").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("email", Table.ColumnType.VarChar, length: 255, notNull: true, unique: true),
                new Table.ColumnDefinition("department_id", Table.ColumnType.Integer, references: "public.departments(id)"),
                new Table.ColumnDefinition("salary", Table.ColumnType.Numeric, precision: 10, scale: 2, defaultValue: "0"),
                new Table.ColumnDefinition("hire_date", Table.ColumnType.Date, notNull: true, defaultValue: "CURRENT_DATE"),
                new Table.ColumnDefinition("is_active", Table.ColumnType.Boolean, notNull: true, defaultValue: "true")
            );

            // 3. categories
            db.Table("public.categories").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("parent_id", Table.ColumnType.Integer, references: "public.categories(id)")
            );

            // 4. products
            db.Table("public.products").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("price", Table.ColumnType.Numeric, precision: 10, scale: 2, notNull: true, defaultValue: "0"),
                new Table.ColumnDefinition("category_id", Table.ColumnType.Integer, references: "public.categories(id)"),
                new Table.ColumnDefinition("is_available", Table.ColumnType.Boolean, notNull: true, defaultValue: "true")
            );

            // 5. orders
            db.Table("public.orders").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("employee_id", Table.ColumnType.Integer, references: "public.employees(id)"),
                new Table.ColumnDefinition("order_date", Table.ColumnType.Timestamp, notNull: true, defaultValue: "NOW()"),
                new Table.ColumnDefinition("total", Table.ColumnType.Numeric, precision: 10, scale: 2, defaultValue: "0"),
                new Table.ColumnDefinition("status", Table.ColumnType.VarChar, length: 50, notNull: true, defaultValue: "'pending'")
            );

            // 6. order_items
            db.Table("public.order_items").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("order_id", Table.ColumnType.Integer, notNull: true, references: "public.orders(id)"),
                new Table.ColumnDefinition("product_id", Table.ColumnType.Integer, notNull: true, references: "public.products(id)"),
                new Table.ColumnDefinition("quantity", Table.ColumnType.Integer, notNull: true, defaultValue: "1"),
                new Table.ColumnDefinition("unit_price", Table.ColumnType.Numeric, precision: 10, scale: 2, notNull: true, defaultValue: "0")
            );

            // 7. projects
            db.Table("public.projects").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("budget", Table.ColumnType.Numeric, precision: 12, scale: 2, defaultValue: "0")
            );

            // 8. employee_projects
            db.Table("public.employee_projects").CreateIfNotExists(
                new Table.ColumnDefinition("employee_id", Table.ColumnType.Integer, notNull: true, references: "public.employees(id)"),
                new Table.ColumnDefinition("project_id", Table.ColumnType.Integer, notNull: true, references: "public.projects(id)"),
                new Table.ColumnDefinition("role", Table.ColumnType.VarChar, length: 100, notNull: true, defaultValue: "'member'")
            );

            // 9. addresses
            db.Table("public.addresses").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("employee_id", Table.ColumnType.Integer, notNull: true, references: "public.employees(id)"),
                new Table.ColumnDefinition("city", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("street", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("is_primary", Table.ColumnType.Boolean, notNull: true, defaultValue: "true")
            );

            // 10. tags
            db.Table("public.tags").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("name", Table.ColumnType.VarChar, length: 100, notNull: true, unique: true)
            );

            // 11. product_tags
            db.Table("public.product_tags").CreateIfNotExists(
                new Table.ColumnDefinition("product_id", Table.ColumnType.Integer, notNull: true, references: "public.products(id)"),
                new Table.ColumnDefinition("tag_id", Table.ColumnType.Integer, notNull: true, references: "public.tags(id)")
            );

            // 12. audit_logs
            db.Table("public.audit_logs").CreateIfNotExists(
                new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
                new Table.ColumnDefinition("employee_id", Table.ColumnType.Integer, references: "public.employees(id)"),
                new Table.ColumnDefinition("action", Table.ColumnType.VarChar, length: 255, notNull: true),
                new Table.ColumnDefinition("details", Table.ColumnType.Text),
                new Table.ColumnDefinition("created_at", Table.ColumnType.Timestamp, notNull: true, defaultValue: "NOW()")
            );
        }
    }
}
