using System.Data;
using System.Data.Common;
using System.Text;
using Titanic.Db.Configuration;
using Titanic.Db.Interfaces;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Базовый провайдер к БД.
    /// Провайдер отвечает за создание подключения, команд и нативных параметров БД.
    /// </summary>
    public abstract class BaseDbProvider
    {
        /// <summary>
        /// Строка подключения.
        /// </summary>
        protected readonly string _connectionString;

        /// <summary>
        /// Пул подключений (опционально).
        /// </summary>
        private BaseDbConnectionPool? _pool;

        /// <summary>
        /// Пул подключений, используемый провайдером.
        /// </summary>
        internal BaseDbConnectionPool? Pool => _pool;

        /// <summary>
        /// Движок SQL-диалекта провайдера.
        /// </summary>
        public BaseDbEngine Engine { get; }

        /// <summary>
        /// Строка подключения.
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="connectionString"> Строка подключения к БД. </param>
        /// <param name="engine"> Движок SQL-диалекта. </param>
        protected BaseDbProvider(string connectionString, BaseDbEngine engine)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Конструктор с настройкой пула подключений.
        /// </summary>
        protected BaseDbProvider(string connectionString, BaseDbEngine engine, ConnectionPoolConfig? poolConfig)
            : this(connectionString, engine)
        {
            if (poolConfig != null)
            {
                _pool = new BaseDbConnectionPool(
                    factory: CreateConnection,
                    config: poolConfig);
            }
        }

        /// <summary>
        /// Подключить пул подключений после создания провайдера.
        /// </summary>
        protected void EnablePool(ConnectionPoolConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _pool ??= new BaseDbConnectionPool(
                factory: CreateConnection,
                config: config);
        }

        /// <summary>
        /// Создать подключение.
        /// </summary>
        protected abstract DbConnection CreateConnection();

        /// <summary>
        /// Создать параметр команды для конкретного провайдера.
        /// </summary>
        /// <param name="parameter"> Универсальное описание параметра. </param>
        protected abstract DbParameter CreateParameter(QueryParameter parameter);

        /// <summary>
        /// Создать билдер SELECT-запроса, связанный с текущим провайдером.
        /// </summary>
        /// <returns>Новый экземпляр билдера <see cref="Select"/>.</returns>
        public virtual Select Select()
        {
            return new(this);
        }

        /// <summary>
        /// Создать билдер SELECT-запроса и сразу добавить в него список выбираемых колонок.
        /// </summary>
        /// <param name="columns">Имена колонок, которые должны попасть в секцию SELECT.</param>
        /// <returns>Новый экземпляр билдера <see cref="Select"/> с добавленными колонками.</returns>
        public virtual Select Select(params string[] columns)
        {
            return new Select(this).Columns(columns);
        }

        /// <summary>
        /// Создать билдер UPDATE-запроса, связанный с текущим провайдером.
        /// </summary>
        /// <returns>Новый экземпляр билдера <see cref="Update"/>.</returns>
        public virtual Update Update()
        {
            return new(this);
        }

        /// <summary>
        /// Создать билдер UPDATE-запроса и сразу указать обновляемую таблицу.
        /// </summary>
        /// <param name="tableName">Имя таблицы, которая будет обновляться.</param>
        /// <returns>Новый экземпляр билдера <see cref="Update"/> с указанной таблицей.</returns>
        public virtual Update Update(string tableName)
        {
            return new Update(this).Table(tableName);
        }

        /// <summary>
        /// Создать билдер DELETE-запроса, связанный с текущим провайдером.
        /// </summary>
        /// <returns>Новый экземпляр билдера <see cref="Delete"/>.</returns>
        public virtual Delete Delete()
        {
            return new(this);
        }

        /// <summary>
        /// Создать билдер DELETE-запроса и сразу указать таблицу-источник.
        /// </summary>
        /// <param name="tableName">Имя таблицы, из которой будут удаляться строки.</param>
        /// <returns>Новый экземпляр билдера <see cref="Delete"/> с указанной таблицей.</returns>
        public virtual Delete Delete(string tableName)
        {
            return new Delete(this).From(tableName);
        }

        /// <summary>
        /// Создать билдер INSERT-запроса, связанный с текущим провайдером.
        /// </summary>
        /// <returns>Новый экземпляр билдера <see cref="InsertSelect"/>.</returns>
        public virtual InsertSelect Insert()
        {
            return new(this);
        }

        /// <summary>
        /// Создать билдер INSERT-запроса и сразу указать целевую таблицу.
        /// </summary>
        /// <param name="tableName">Имя таблицы, в которую будут вставляться данные.</param>
        /// <returns>Новый экземпляр билдера <see cref="InsertSelect"/> с указанной таблицей.</returns>
        public virtual InsertSelect Insert(string tableName)
        {
            return new InsertSelect(this).Into(tableName);
        }

        /// <summary>
        /// Создать DDL-билдер таблицы для текущего провайдера.
        /// </summary>
        /// <param name="tableName">Имя таблицы, для которой будет создан DDL-билдер.</param>
        /// <returns>Новый экземпляр билдера <see cref="Table"/>.</returns>
        public virtual Table Table(string tableName)
        {
            return new(this, tableName);
        }

        /// <summary>
        /// Создать DDL-билдер таблицы для CLR-модели с атрибутами таблицы.
        /// </summary>
        /// <typeparam name="TModel">Тип CLR-модели, из которой будет получено имя таблицы.</typeparam>
        /// <returns>Новый экземпляр билдера <see cref="Table"/> для таблицы модели.</returns>
        public virtual Table Table<TModel>()
        {
            return new(this, Titanic.Db.Table.ResolveTableName<TModel>());
        }

        #region Table DDL

        /// <summary>
        /// Выполнить CREATE TABLE для текущего провайдера.
        /// </summary>
        public virtual int ExecuteCreateTable(
            string tableName,
            IReadOnlyCollection<Titanic.Db.Table.ColumnDefinition> columns,
            bool ifNotExists,
            IReadOnlyCollection<Titanic.Db.Table.TableConstraint>? constraints = null)
        {
            return Execute(BuildCreateTableSql(tableName, columns, ifNotExists, constraints));
        }

        /// <summary>
        /// Выполнить DROP TABLE для текущего провайдера.
        /// </summary>
        public virtual int ExecuteDropTable(string tableName, bool ifExists = false, bool cascade = false)
        {
            var ifStr = ifExists ? " IF EXISTS" : "";
            var cascadeStr = cascade ? " CASCADE" : "";
            return Execute($"DROP TABLE{ifStr} {Engine.QuoteObjectName(tableName)}{cascadeStr}");
        }

        /// <summary>
        /// Выполнить TRUNCATE TABLE для текущего провайдера.
        /// </summary>
        public virtual int ExecuteTruncateTable(string tableName, bool restartIdentity = false, bool cascade = false)
        {
            var restart = restartIdentity ? " RESTART IDENTITY" : "";
            var cascadeStr = cascade ? " CASCADE" : "";
            return Execute($"TRUNCATE TABLE {Engine.QuoteObjectName(tableName)}{restart}{cascadeStr}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ADD COLUMN для текущего провайдера.
        /// </summary>
        public virtual int ExecuteAddColumn(
            string tableName,
            Titanic.Db.Table.ColumnDefinition column,
            bool ifNotExists = false)
        {
            var ifStr = ifNotExists ? " IF NOT EXISTS" : "";
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} ADD COLUMN{ifStr} {BuildColumnDefinitionSql(column)}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE DROP COLUMN для текущего провайдера.
        /// </summary>
        public virtual int ExecuteDropColumn(string tableName, string columnName)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} DROP COLUMN {Engine.QuoteIdentifier(columnName)}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE RENAME COLUMN для текущего провайдера.
        /// </summary>
        public virtual int ExecuteRenameColumn(string tableName, string oldName, string newName)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} RENAME COLUMN {Engine.QuoteIdentifier(oldName)} TO {Engine.QuoteIdentifier(newName)}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ALTER COLUMN TYPE для текущего провайдера.
        /// </summary>
        public virtual int ExecuteAlterColumnType(string tableName, string columnName, Titanic.Db.Table.ColumnType newType)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} ALTER COLUMN {Engine.QuoteIdentifier(columnName)} TYPE {BuildColumnTypeSql(new Titanic.Db.Table.ColumnDefinition(columnName, newType))}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ALTER COLUMN SET DEFAULT для текущего провайдера.
        /// </summary>
        public virtual int ExecuteSetDefault(string tableName, string columnName, string defaultValue)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} ALTER COLUMN {Engine.QuoteIdentifier(columnName)} SET DEFAULT {defaultValue}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ALTER COLUMN DROP DEFAULT для текущего провайдера.
        /// </summary>
        public virtual int ExecuteDropDefault(string tableName, string columnName)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} ALTER COLUMN {Engine.QuoteIdentifier(columnName)} DROP DEFAULT");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ALTER COLUMN SET NOT NULL для текущего провайдера.
        /// </summary>
        public virtual int ExecuteSetNotNull(string tableName, string columnName)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} ALTER COLUMN {Engine.QuoteIdentifier(columnName)} SET NOT NULL");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ALTER COLUMN DROP NOT NULL для текущего провайдера.
        /// </summary>
        public virtual int ExecuteDropNotNull(string tableName, string columnName)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} ALTER COLUMN {Engine.QuoteIdentifier(columnName)} DROP NOT NULL");
        }

        /// <summary>
        /// Выполнить CREATE INDEX для текущего провайдера.
        /// </summary>
        public virtual int ExecuteCreateIndex(
            string tableName,
            string indexName,
            IReadOnlyCollection<string> columns,
            bool unique = false,
            bool ifNotExists = false)
        {
            var uniqueStr = unique ? "UNIQUE " : "";
            var ifStr = ifNotExists ? " IF NOT EXISTS" : "";
            var columnSql = string.Join(", ", columns.Select(Engine.QuoteIdentifier));
            return Execute(
                $"CREATE {uniqueStr}INDEX{ifStr} {Engine.QuoteObjectName(indexName)} ON {Engine.QuoteObjectName(tableName)} ({columnSql})");
        }

        /// <summary>
        /// Выполнить DROP INDEX для текущего провайдера.
        /// </summary>
        public virtual int ExecuteDropIndex(string indexName, bool ifExists = false)
        {
            var ifStr = ifExists ? " IF EXISTS" : "";
            return Execute($"DROP INDEX{ifStr} {Engine.QuoteObjectName(indexName)}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE RENAME TO для текущего провайдера.
        /// </summary>
        public virtual int ExecuteRenameTable(string tableName, string newName)
        {
            return Execute(
                $"ALTER TABLE {Engine.QuoteObjectName(tableName)} RENAME TO {Engine.QuoteIdentifier(newName)}");
        }

        /// <summary>
        /// Выполнить ALTER TABLE ADD CONSTRAINT FOREIGN KEY, если constraint ещё не существует.
        /// </summary>
        public virtual int ExecuteAddForeignKeyIfNotExists(
            string tableName,
            string constraintName,
            string columnName,
            string referenceTableName,
            string referenceColumnName = "id")
        {
            if (TableConstraintExists(tableName, constraintName))
            {
                return 0;
            }

            return Execute(
                $"""
                ALTER TABLE {Engine.QuoteObjectName(tableName)}
                ADD CONSTRAINT {Engine.QuoteIdentifier(constraintName)}
                FOREIGN KEY ({Engine.QuoteIdentifier(columnName)})
                REFERENCES {Engine.QuoteObjectName(referenceTableName)}({Engine.QuoteIdentifier(referenceColumnName)})
                """);
        }

        /// <summary>
        /// Получить количество строк в таблице.
        /// </summary>
        public virtual long ExecuteTableRowCount(string tableName)
        {
            return ExecuteScalar<long>($"SELECT COUNT(*) FROM {Engine.QuoteObjectName(tableName)}");
        }

        /// <summary>
        /// Проверить наличие constraint у таблицы.
        /// </summary>
        public virtual bool TableConstraintExists(string tableName, string constraintName)
        {
            var (schema, table) = SplitSchemaAndTable(tableName);
            var result = Select()
                .Column(Column.Const(1))
                .From("information_schema.table_constraints").As("tc")
                .Where("tc", "table_schema").IsEqual(Column.Parameter(schema))
                .And("tc", "table_name").IsEqual(Column.Parameter(table))
                .And("tc", "constraint_name").IsEqual(Column.Parameter(constraintName))
                .Limit(1)
                .Take(1)
                .ExecuteScalar<int>();

            return result == 1;
        }

        /// <summary>
        /// Построить SQL CREATE TABLE.
        /// </summary>
        protected virtual string BuildCreateTableSql(
            string tableName,
            IReadOnlyCollection<Titanic.Db.Table.ColumnDefinition> columns,
            bool ifNotExists,
            IReadOnlyCollection<Titanic.Db.Table.TableConstraint>? constraints = null)
        {
            if (columns.Count == 0)
            {
                throw new ArgumentException("Table must contain at least one column.", nameof(columns));
            }

            var items = columns
                .Select(BuildColumnDefinitionSql)
                .ToList();

            var primaryKeyColumns = columns
                .Where(column => column.PrimaryKey)
                .Select(column => column.Name)
                .ToArray();
            if (primaryKeyColumns.Length > 0)
            {
                items.Add(BuildTableConstraintSql(Titanic.Db.Table.TableConstraint.PrimaryKey(primaryKeyColumns)));
            }

            if (constraints != null)
            {
                items.AddRange(constraints.Select(BuildTableConstraintSql));
            }

            var ifStr = ifNotExists ? " IF NOT EXISTS" : "";
            var builder = new StringBuilder();
            builder.Append("CREATE TABLE").Append(ifStr).Append(' ').Append(Engine.QuoteObjectName(tableName));
            builder.Append(" (").AppendLine();
            builder.Append(Engine.Indent).Append(string.Join($",{Environment.NewLine}{Engine.Indent}", items));
            builder.AppendLine();
            builder.Append(')');
            return builder.ToString();
        }

        /// <summary>
        /// Построить SQL-описание колонки.
        /// </summary>
        protected virtual string BuildColumnDefinitionSql(Titanic.Db.Table.ColumnDefinition column)
        {
            ArgumentNullException.ThrowIfNull(column);

            var builder = new StringBuilder();
            builder.Append(Engine.QuoteIdentifier(column.Name)).Append(' ').Append(BuildColumnTypeSql(column));
            if (column.NotNull)
            {
                builder.Append(" NOT NULL");
            }

            if (column.DefaultValue != null)
            {
                builder.Append(" DEFAULT ").Append(column.DefaultValue);
            }

            if (column.Unique)
            {
                builder.Append(" UNIQUE");
            }

            if (column.References != null)
            {
                builder.Append(" REFERENCES ").Append(column.References);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Построить SQL-тип колонки.
        /// </summary>
        protected virtual string BuildColumnTypeSql(Titanic.Db.Table.ColumnDefinition column)
        {
            return column.Type switch
            {
                Titanic.Db.Table.ColumnType.Serial => "SERIAL",
                Titanic.Db.Table.ColumnType.Integer => "INTEGER",
                Titanic.Db.Table.ColumnType.BigInt => "BIGINT",
                Titanic.Db.Table.ColumnType.VarChar => $"VARCHAR({column.Length})",
                Titanic.Db.Table.ColumnType.Text => "TEXT",
                Titanic.Db.Table.ColumnType.Boolean => "BOOLEAN",
                Titanic.Db.Table.ColumnType.Numeric => $"NUMERIC({column.Precision}, {column.Scale})",
                Titanic.Db.Table.ColumnType.Date => "DATE",
                Titanic.Db.Table.ColumnType.Timestamp => "TIMESTAMP",
                Titanic.Db.Table.ColumnType.TimestampTz => "TIMESTAMPTZ",
                Titanic.Db.Table.ColumnType.Uuid => "UUID",
                Titanic.Db.Table.ColumnType.Jsonb => "JSONB",
                _ => throw new NotSupportedException($"Column type {column.Type} is not supported")
            };
        }

        /// <summary>
        /// Построить SQL-описание table-level constraint.
        /// </summary>
        protected virtual string BuildTableConstraintSql(Titanic.Db.Table.TableConstraint constraint)
        {
            if (constraint.Columns.Count == 0)
            {
                throw new ArgumentException("Table constraint must contain at least one column.", nameof(constraint));
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(constraint.Name))
            {
                builder.Append("CONSTRAINT ").Append(Engine.QuoteIdentifier(constraint.Name)).Append(' ');
            }

            builder.Append(constraint.Type switch
            {
                Titanic.Db.Table.ConstraintType.PrimaryKey => "PRIMARY KEY",
                Titanic.Db.Table.ConstraintType.Unique => "UNIQUE",
                _ => throw new NotSupportedException($"Constraint type {constraint.Type} is not supported")
            });
            builder.Append(" (");
            builder.Append(string.Join(", ", constraint.Columns.Select(Engine.QuoteIdentifier)));
            builder.Append(')');
            return builder.ToString();
        }

        /// <summary>
        /// Разделить имя таблицы на схему и имя таблицы.
        /// </summary>
        protected static (string Schema, string Table) SplitSchemaAndTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("Table name is empty.", nameof(tableName));
            }

            var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length == 2
                ? (parts[0], parts[1])
                : ("public", tableName);
        }

        #endregion Table DDL
        /// <summary>
        /// Сформировать SQL и параметры запроса под текущий провайдер.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        public virtual QueryBuildResult Build(IQuery query)
        {
            if (query is BaseQuery baseQuery)
            {
                baseQuery.UseProvider(this);
            }

            return query.Build();
        }

        /// <summary>
        /// Выполнить запрос без чтения результата.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        public virtual int Execute(IQuery query)
        {
            var result = Build(query);
            return Execute(result.Sql, result.Parameters);
        }

        /// <summary>
        /// Выполнить SQL без чтения результата.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        public virtual int Execute(string sql, IEnumerable<QueryParameter>? parameters = null)
        {
            using var rental = RentConnection(out var connection);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);

            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Выполнить запрос с чтением строк.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        public virtual void Execute(IQuery query, Action<DbDataReader> handleRow)
        {
            var result = Build(query);
            Execute(result.Sql, result.Parameters, handleRow);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк через IDataReader.
        /// </summary>
        /// <param name="query"> Запрос. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        public virtual void ExecuteReader(IQuery query, Action<IDataReader> handleRow)
        {
            ArgumentNullException.ThrowIfNull(handleRow);
            Execute(query, reader => handleRow(reader));
        }

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        public virtual List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
        {
            ArgumentNullException.ThrowIfNull(mapRow);

            return Query(query, mapRow);
        }

        /// <summary>
        /// Выполнить запрос с чтением строк и вернуть готовый список результатов через IDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        public virtual List<T> ExecuteReader<T>(IQuery query, Func<IDataReader, T> mapRow)
        {
            ArgumentNullException.ThrowIfNull(mapRow);

            var rows = new List<T>();
            ExecuteReader(query, reader => rows.Add(mapRow(reader)));
            return rows;
        }

        /// <summary>
        /// Выполнить SQL с чтением строк через IDataReader.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        public virtual void ExecuteReader(string sql, IEnumerable<QueryParameter>? parameters, Action<IDataReader> handleRow)
        {
            ArgumentNullException.ThrowIfNull(handleRow);
            Execute(sql, parameters, reader => handleRow(reader));
        }

        /// <summary>
        /// Выполнить SQL с чтением строк и вернуть готовый список результатов через DbDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        public virtual List<T> ExecuteReader<T>(string sql, IEnumerable<QueryParameter>? parameters, Func<DbDataReader, T> mapRow)
        {
            ArgumentNullException.ThrowIfNull(mapRow);

            var rows = new List<T>();
            Execute(sql, parameters, reader => rows.Add(mapRow(reader)));
            return rows;
        }

        /// <summary>
        /// Выполнить SQL с чтением строк и вернуть готовый список результатов через IDataReader.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        public virtual List<T> ExecuteReader<T>(string sql, IEnumerable<QueryParameter>? parameters, Func<IDataReader, T> mapRow)
        {
            ArgumentNullException.ThrowIfNull(mapRow);

            var rows = new List<T>();
            ExecuteReader(sql, parameters, reader => rows.Add(mapRow(reader)));
            return rows;
        }

        /// <summary>
        /// Выполнить SQL с чтением строк.
        /// </summary>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        /// <param name="handleRow"> Обработчик строки. </param>
        public virtual void Execute(string sql, IEnumerable<QueryParameter>? parameters, Action<DbDataReader> handleRow)
        {
            ArgumentNullException.ThrowIfNull(handleRow);
            using var rental = RentConnection(out var connection);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                handleRow(reader);
            }
        }

        /// <summary>
        /// Выполнить запрос и преобразовать строки результата.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        /// <param name="mapRow"> Преобразователь строки. </param>
        public virtual List<T> Query<T>(IQuery query, System.Func<DbDataReader, T> mapRow)
        {
            ArgumentNullException.ThrowIfNull(mapRow);

            var rows = new List<T>();
            Execute(query, reader => rows.Add(mapRow(reader)));
            return rows;
        }

        /// <summary>
        /// Выполнить запрос с получением одного значения.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="query"> Запрос. </param>
        public virtual T? ExecuteScalar<T>(IQuery query)
        {
            var result = Build(query);
            return ExecuteScalar<T>(result.Sql, result.Parameters);
        }

        /// <summary>
        /// Выполнить SQL с получением одного значения.
        /// </summary>
        /// <typeparam name="T"> Тип результата. </typeparam>
        /// <param name="sql"> SQL текст. </param>
        /// <param name="parameters"> Параметры. </param>
        public virtual T? ExecuteScalar<T>(string sql, IEnumerable<QueryParameter>? parameters = null)
        {
            using var rental = RentConnection(out var connection);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);

            return DbValueConverter.ConvertTo<T>(command.ExecuteScalar());
        }

        private void AddParameters(DbCommand command, IEnumerable<QueryParameter>? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(CreateParameter(parameter));
            }
        }

        /// <summary>
        /// Получить подключение из пула или создать новое.
        /// </summary>
        private PooledConnection RentConnection(out DbConnection connection)
        {
            if (_pool != null)
            {
                connection = _pool.Rent();
                return new PooledConnection(this, connection, fromPool: true);
            }

            connection = CreateConnection();
            return new PooledConnection(this, connection, fromPool: false);
        }

        /// <summary>
        /// Возвращает подключение в пул или закрывает его.
        /// </summary>
        private void ReleaseConnection(DbConnection connection, bool fromPool)
        {
            try
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            catch
            {
                // ignore
            }

            if (fromPool && _pool != null)
            {
                _pool.Return(connection);
            }
            else
            {
                connection.Dispose();
            }
        }

        /// <summary>
        /// RAII-обёртка: возвращает подключение в пул при Dispose.
        /// </summary>
        private readonly struct PooledConnection : IDisposable
        {
            private readonly BaseDbProvider _owner;
            private readonly DbConnection _connection;
            private readonly bool _fromPool;

            internal PooledConnection(BaseDbProvider owner, DbConnection connection, bool fromPool)
            {
                _owner = owner;
                _connection = connection;
                _fromPool = fromPool;
            }

            /// <summary>
            /// Освободить обёрнутое подключение: вернуть его владельцу в пул
            /// или закрыть, если подключение не было получено из пула.
            /// </summary>
            public void Dispose()
            {
                _owner.ReleaseConnection(_connection, _fromPool);
            }
        }
    }
}
