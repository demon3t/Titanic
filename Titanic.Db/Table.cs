using System.Reflection;
using Titanic.Db.Abstractions;
using Titanic.Db.Attributes;

namespace Titanic.Db
{
    /// <summary>
    /// DDL-фасад для работы с таблицей через текущий провайдер БД.
    /// SQL конкретного диалекта формируется провайдером, а не этим классом.
    /// </summary>
    public class Table
    {
        #region Fields

        private readonly BaseDbProvider _provider;
        private readonly string _tableName;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Создать DDL-фасад таблицы.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="tableName"> Имя таблицы с опциональной схемой. </param>
        public Table(BaseDbProvider provider, string tableName)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _tableName = string.IsNullOrWhiteSpace(tableName)
                ? throw new ArgumentException("Table name is empty.", nameof(tableName))
                : tableName;
        }

        /// <summary>
        /// Создать DDL-фасад таблицы из объекта базы данных.
        /// </summary>
        /// <param name="database"> База данных. </param>
        /// <param name="tableName"> Имя таблицы. </param>
        public Table(Database database, string tableName)
            : this(database.Provider, tableName)
        {
        }

        #endregion Constructors

        #region Static Methods

        /// <summary>
        /// Получить имя таблицы из атрибута CLR-модели.
        /// </summary>
        /// <typeparam name="TModel"> Тип CLR-модели таблицы. </typeparam>
        /// <returns> Имя таблицы. </returns>
        public static string ResolveTableName<TModel>()
        {
            return ResolveTableName(typeof(TModel));
        }

        /// <summary>
        /// Получить имя таблицы из атрибута CLR-модели.
        /// </summary>
        /// <param name="modelType"> Тип CLR-модели таблицы. </param>
        /// <returns> Имя таблицы. </returns>
        public static string ResolveTableName(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            return modelType.GetCustomAttribute<DbTableAttribute>()?.Name
                ?? modelType.Name;
        }

        #endregion Static Methods
        #region Definitions

        /// <summary>
        /// SQL-типы колонок для DDL.
        /// </summary>
        public enum ColumnType
        {
            /// <summary> Автоинкрементное целое значение. </summary>
            Serial,

            /// <summary> Целое значение. </summary>
            Integer,

            /// <summary> Большое целое значение. </summary>
            BigInt,

            /// <summary> Строка ограниченной длины. </summary>
            VarChar,

            /// <summary> Текст. </summary>
            Text,

            /// <summary> Логическое значение. </summary>
            Boolean,

            /// <summary> Число с точностью и масштабом. </summary>
            Numeric,

            /// <summary> Дата. </summary>
            Date,

            /// <summary> Дата и время. </summary>
            Timestamp,

            /// <summary> Дата и время с часовым поясом. </summary>
            TimestampTz,

            /// <summary> UUID. </summary>
            Uuid,

            /// <summary> JSONB. </summary>
            Jsonb
        }

        /// <summary>
        /// Тип table-level constraint.
        /// </summary>
        public enum ConstraintType
        {
            /// <summary> PRIMARY KEY. </summary>
            PrimaryKey,

            /// <summary> UNIQUE. </summary>
            Unique
        }

        /// <summary>
        /// Описание колонки для DDL.
        /// </summary>
        public sealed class ColumnDefinition
        {
            #region Constructors

            /// <summary>
            /// Создать описание колонки.
            /// </summary>
            /// <param name="name"> Имя колонки. </param>
            /// <param name="type"> Тип колонки. </param>
            /// <param name="length"> Длина для VARCHAR. </param>
            /// <param name="precision"> Precision для NUMERIC. </param>
            /// <param name="scale"> Scale для NUMERIC. </param>
            /// <param name="notNull"> Признак NOT NULL. </param>
            /// <param name="primaryKey"> Признак PRIMARY KEY. </param>
            /// <param name="unique"> Признак UNIQUE. </param>
            /// <param name="defaultValue"> SQL-выражение значения по умолчанию. </param>
            /// <param name="references"> SQL-выражение REFERENCES. </param>
            public ColumnDefinition(
                string name,
                ColumnType type,
                int length = 255,
                int precision = 10,
                int scale = 2,
                bool notNull = false,
                bool primaryKey = false,
                bool unique = false,
                string? defaultValue = null,
                string? references = null)
            {
                Name = string.IsNullOrWhiteSpace(name)
                    ? throw new ArgumentException("Column name is empty.", nameof(name))
                    : name;
                Type = type;
                Length = length;
                Precision = precision;
                Scale = scale;
                NotNull = notNull;
                PrimaryKey = primaryKey;
                Unique = unique;
                DefaultValue = defaultValue;
                References = references;
            }

            #endregion Constructors

            #region Properties

            /// <summary>
            /// Имя колонки.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Тип колонки.
            /// </summary>
            public ColumnType Type { get; }

            /// <summary>
            /// Длина для VARCHAR.
            /// </summary>
            public int Length { get; }

            /// <summary>
            /// Precision для NUMERIC.
            /// </summary>
            public int Precision { get; }

            /// <summary>
            /// Scale для NUMERIC.
            /// </summary>
            public int Scale { get; }

            /// <summary>
            /// Признак NOT NULL.
            /// </summary>
            public bool NotNull { get; }

            /// <summary>
            /// Признак PRIMARY KEY.
            /// </summary>
            public bool PrimaryKey { get; }

            /// <summary>
            /// Признак UNIQUE.
            /// </summary>
            public bool Unique { get; }

            /// <summary>
            /// SQL-выражение значения по умолчанию.
            /// </summary>
            public string? DefaultValue { get; }

            /// <summary>
            /// SQL-выражение REFERENCES.
            /// </summary>
            public string? References { get; }

            #endregion Properties
        }

        /// <summary>
        /// Описание table-level constraint.
        /// </summary>
        public sealed class TableConstraint
        {
            #region Constructors

            private TableConstraint(string? name, ConstraintType type, IReadOnlyList<string> columns)
            {
                if (columns.Count == 0)
                {
                    throw new ArgumentException("Constraint must contain at least one column.", nameof(columns));
                }

                Name = name;
                Type = type;
                Columns = columns;
            }

            #endregion Constructors

            #region Properties

            /// <summary>
            /// Имя constraint.
            /// </summary>
            public string? Name { get; }

            /// <summary>
            /// Тип constraint.
            /// </summary>
            public ConstraintType Type { get; }

            /// <summary>
            /// Колонки constraint.
            /// </summary>
            public IReadOnlyList<string> Columns { get; }

            #endregion Properties

            #region Factory Methods

            /// <summary>
            /// Создать PRIMARY KEY constraint.
            /// </summary>
            /// <param name="columns"> Колонки первичного ключа. </param>
            public static TableConstraint PrimaryKey(params string[] columns)
            {
                return new TableConstraint(null, ConstraintType.PrimaryKey, NormalizeColumns(columns));
            }

            /// <summary>
            /// Создать именованный PRIMARY KEY constraint.
            /// </summary>
            /// <param name="name"> Имя constraint. </param>
            /// <param name="columns"> Колонки первичного ключа. </param>
            public static TableConstraint NamedPrimaryKey(string name, params string[] columns)
            {
                return new TableConstraint(name, ConstraintType.PrimaryKey, NormalizeColumns(columns));
            }

            /// <summary>
            /// Создать UNIQUE constraint.
            /// </summary>
            /// <param name="columns"> Уникальные колонки. </param>
            public static TableConstraint Unique(params string[] columns)
            {
                return new TableConstraint(null, ConstraintType.Unique, NormalizeColumns(columns));
            }

            /// <summary>
            /// Создать именованный UNIQUE constraint.
            /// </summary>
            /// <param name="name"> Имя constraint. </param>
            /// <param name="columns"> Уникальные колонки. </param>
            public static TableConstraint NamedUnique(string name, params string[] columns)
            {
                return new TableConstraint(name, ConstraintType.Unique, NormalizeColumns(columns));
            }

            private static IReadOnlyList<string> NormalizeColumns(IReadOnlyCollection<string> columns)
            {
                return columns
                    .Select(column => string.IsNullOrWhiteSpace(column)
                        ? throw new ArgumentException("Constraint column name is empty.", nameof(columns))
                        : column)
                    .ToArray();
            }

            #endregion Factory Methods
        }

        #endregion Definitions

        #region Create

        /// <summary>
        /// Создать таблицу, если она не существует.
        /// </summary>
        /// <param name="columns"> Определения колонок. </param>
        public int CreateIfNotExists(params ColumnDefinition[] columns)
        {
            return CreateIfNotExists((IEnumerable<ColumnDefinition>)columns);
        }

        /// <summary>
        /// Создать таблицу по атрибутам CLR-модели, если она не существует.
        /// </summary>
        /// <typeparam name="TModel"> Тип CLR-модели таблицы. </typeparam>
        /// <param name="createIndexes"> Создать индексы, описанные атрибутами. </param>
        public int CreateIfNotExists<TModel>(bool createIndexes = true)
        {
            return CreateIfNotExists(typeof(TModel), createIndexes);
        }

        /// <summary>
        /// Создать таблицу по атрибутам CLR-модели, если она не существует.
        /// </summary>
        /// <param name="modelType"> Тип CLR-модели таблицы. </param>
        /// <param name="createIndexes"> Создать индексы, описанные атрибутами. </param>
        public int CreateIfNotExists(Type modelType, bool createIndexes = true)
        {
            var result = CreateIfNotExists(ReadColumns(modelType), ReadConstraints(modelType));
            if (createIndexes)
            {
                CreateIndexes(modelType);
            }

            return result;
        }
        /// <summary>
        /// Создать таблицу, если она не существует.
        /// </summary>
        /// <param name="columns"> Определения колонок. </param>
        /// <param name="constraints"> Ограничения уровня таблицы. </param>
        public int CreateIfNotExists(IEnumerable<ColumnDefinition> columns, params TableConstraint[] constraints)
        {
            return _provider.ExecuteCreateTable(_tableName, columns.ToArray(), ifNotExists: true, constraints);
        }

        /// <summary>
        /// Создать таблицу, если она не существует.
        /// </summary>
        /// <param name="columns"> Определения колонок. </param>
        public int Create(params ColumnDefinition[] columns)
        {
            return CreateIfNotExists(columns);
        }

        /// <summary>
        /// Создать таблицу по атрибутам CLR-модели, если она не существует.
        /// </summary>
        /// <typeparam name="TModel"> Тип CLR-модели таблицы. </typeparam>
        /// <param name="createIndexes"> Создать индексы, описанные атрибутами. </param>
        public int Create<TModel>(bool createIndexes = true)
        {
            return CreateIfNotExists<TModel>(createIndexes);
        }

        /// <summary>
        /// Создать таблицу по атрибутам CLR-модели, если она не существует.
        /// </summary>
        /// <param name="modelType"> Тип CLR-модели таблицы. </param>
        /// <param name="createIndexes"> Создать индексы, описанные атрибутами. </param>
        public int Create(Type modelType, bool createIndexes = true)
        {
            return CreateIfNotExists(modelType, createIndexes);
        }
        /// <summary>
        /// Создать таблицу, если она не существует.
        /// </summary>
        /// <param name="columns"> Определения колонок. </param>
        /// <param name="constraints"> Ограничения уровня таблицы. </param>
        public int Create(IEnumerable<ColumnDefinition> columns, params TableConstraint[] constraints)
        {
            return CreateIfNotExists(columns, constraints);
        }

        #endregion Create

        #region Drop And Truncate

        /// <summary>
        /// Удалить таблицу.
        /// </summary>
        /// <param name="ifExists"> Добавить IF EXISTS. </param>
        /// <param name="cascade"> Добавить CASCADE. </param>
        public int Drop(bool ifExists = false, bool cascade = false)
        {
            return _provider.ExecuteDropTable(_tableName, ifExists, cascade);
        }

        /// <summary>
        /// Очистить таблицу.
        /// </summary>
        /// <param name="restartIdentity"> Добавить RESTART IDENTITY. </param>
        /// <param name="cascade"> Добавить CASCADE. </param>
        public int Truncate(bool restartIdentity = false, bool cascade = false)
        {
            return _provider.ExecuteTruncateTable(_tableName, restartIdentity, cascade);
        }

        #endregion Drop And Truncate

        #region Columns

        /// <summary>
        /// Добавить колонку.
        /// </summary>
        /// <param name="column"> Описание колонки. </param>
        /// <param name="ifNotExists"> Добавить IF NOT EXISTS. </param>
        public int AddColumn(ColumnDefinition column, bool ifNotExists = false)
        {
            return _provider.ExecuteAddColumn(_tableName, column, ifNotExists);
        }

        /// <summary>
        /// Добавить колонку с простым типом.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        /// <param name="columnType"> Тип колонки. </param>
        /// <param name="defaultValue"> SQL-выражение значения по умолчанию. </param>
        /// <param name="ifNotExists"> Добавить IF NOT EXISTS. </param>
        public int AddColumn(
            string columnName,
            ColumnType columnType,
            string? defaultValue = null,
            bool ifNotExists = false)
        {
            return AddColumn(new ColumnDefinition(columnName, columnType, defaultValue: defaultValue), ifNotExists);
        }

        /// <summary>
        /// Удалить колонку.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        public int DropColumn(string columnName)
        {
            return _provider.ExecuteDropColumn(_tableName, columnName);
        }

        /// <summary>
        /// Переименовать колонку.
        /// </summary>
        /// <param name="oldName"> Текущее имя. </param>
        /// <param name="newName"> Новое имя. </param>
        public int RenameColumn(string oldName, string newName)
        {
            return _provider.ExecuteRenameColumn(_tableName, oldName, newName);
        }

        /// <summary>
        /// Изменить тип колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        /// <param name="newType"> Новый тип. </param>
        public int AlterColumnType(string columnName, ColumnType newType)
        {
            return _provider.ExecuteAlterColumnType(_tableName, columnName, newType);
        }

        /// <summary>
        /// Установить значение по умолчанию для колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        /// <param name="defaultValue"> SQL-выражение значения по умолчанию. </param>
        public int SetDefault(string columnName, string defaultValue)
        {
            return _provider.ExecuteSetDefault(_tableName, columnName, defaultValue);
        }

        /// <summary>
        /// Удалить значение по умолчанию для колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        public int DropDefault(string columnName)
        {
            return _provider.ExecuteDropDefault(_tableName, columnName);
        }

        /// <summary>
        /// Установить NOT NULL для колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        public int SetNotNull(string columnName)
        {
            return _provider.ExecuteSetNotNull(_tableName, columnName);
        }

        /// <summary>
        /// Удалить NOT NULL для колонки.
        /// </summary>
        /// <param name="columnName"> Имя колонки. </param>
        public int DropNotNull(string columnName)
        {
            return _provider.ExecuteDropNotNull(_tableName, columnName);
        }

        #endregion Columns

        #region Constraints And Indexes

        /// <summary>
        /// Добавить foreign key constraint, если он ещё не существует.
        /// </summary>
        /// <param name="constraintName"> Имя constraint. </param>
        /// <param name="columnName"> FK-колонка. </param>
        /// <param name="referenceTableName"> Таблица, на которую ссылается FK. </param>
        /// <param name="referenceColumnName"> Колонка, на которую ссылается FK. </param>
        public int AddForeignKeyIfNotExists(
            string constraintName,
            string columnName,
            string referenceTableName,
            string referenceColumnName = "id")
        {
            return _provider.ExecuteAddForeignKeyIfNotExists(
                _tableName,
                constraintName,
                columnName,
                referenceTableName,
                referenceColumnName);
        }

        /// <summary>
        /// Создать индекс.
        /// </summary>
        /// <param name="indexName"> Имя индекса. </param>
        /// <param name="columns"> Колонки индекса. </param>
        /// <param name="unique"> Создать уникальный индекс. </param>
        public int CreateIndex(string indexName, string[] columns, bool unique = false)
        {
            return _provider.ExecuteCreateIndex(_tableName, indexName, columns, unique);
        }

        /// <summary>
        /// Создать индекс.
        /// </summary>
        /// <param name="indexName"> Имя индекса. </param>
        /// <param name="columns"> Колонки индекса. </param>
        /// <param name="unique"> Создать уникальный индекс. </param>
        /// <param name="ifNotExists"> Добавить IF NOT EXISTS. </param>
        public int CreateIndex(string indexName, string[] columns, bool unique, bool ifNotExists)
        {
            return _provider.ExecuteCreateIndex(_tableName, indexName, columns, unique, ifNotExists);
        }

        /// <summary>
        /// Создать индексы по атрибутам CLR-модели.
        /// </summary>
        /// <typeparam name="TModel"> Тип CLR-модели таблицы. </typeparam>
        /// <param name="ifNotExists"> Добавить IF NOT EXISTS. </param>
        /// <returns> Количество отправленных CREATE INDEX команд. </returns>
        public int CreateIndexes<TModel>(bool ifNotExists = true)
        {
            return CreateIndexes(typeof(TModel), ifNotExists);
        }

        /// <summary>
        /// Создать индексы по атрибутам CLR-модели.
        /// </summary>
        /// <param name="modelType"> Тип CLR-модели таблицы. </param>
        /// <param name="ifNotExists"> Добавить IF NOT EXISTS. </param>
        /// <returns> Количество отправленных CREATE INDEX команд. </returns>
        public int CreateIndexes(Type modelType, bool ifNotExists = true)
        {
            var count = 0;
            foreach (var index in ReadIndexes(modelType))
            {
                CreateIndex(index.Name, index.Columns.ToArray(), index.Unique, ifNotExists);
                count++;
            }

            return count;
        }
        /// <summary>
        /// Создать уникальный индекс.
        /// </summary>
        /// <param name="indexName"> Имя индекса. </param>
        /// <param name="column"> Колонка индекса. </param>
        public int CreateUniqueIndex(string indexName, string column)
        {
            return CreateIndex(indexName, [column], unique: true);
        }

        /// <summary>
        /// Удалить индекс.
        /// </summary>
        /// <param name="indexName"> Имя индекса. </param>
        /// <param name="ifExists"> Добавить IF EXISTS. </param>
        public int DropIndex(string indexName, bool ifExists = false)
        {
            return _provider.ExecuteDropIndex(indexName, ifExists);
        }

        #endregion Constraints And Indexes

        #region Rename

        /// <summary>
        /// Переименовать таблицу.
        /// </summary>
        /// <param name="newName"> Новое имя таблицы. </param>
        public int Rename(string newName)
        {
            return _provider.ExecuteRenameTable(_tableName, newName);
        }

        #endregion Rename

        #region Metadata

        /// <summary>
        /// Проверить, существует ли таблица.
        /// </summary>
        public bool Exists()
        {
            var schema = "public";
            var table = _tableName;
            if (_tableName.Contains('.'))
            {
                var parts = _tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                schema = parts[0];
                table = parts[1];
            }

            var result = _provider.Select()
                .Column(Column.Const(1))
                .From("information_schema.tables").As("t")
                .Where("t", "table_schema").IsEqual(Column.Parameter(schema))
                .And("t", "table_name").IsEqual(Column.Parameter(table))
                .Limit(1)
                .Take(1)
                .ExecuteScalar<int>();

            return result == 1;
        }

        /// <summary>
        /// Получить количество строк в таблице.
        /// </summary>
        public long RowCount()
        {
            return _provider.ExecuteTableRowCount(_tableName);
        }

        #endregion Metadata

        #region Attribute Mapping

        private static ColumnDefinition[] ReadColumns(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            var columns = modelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.GetCustomAttribute<DbColumnAttribute>())
                .Where(attribute => attribute != null)
                .Select(attribute => new ColumnDefinition(
                    attribute!.Name,
                    attribute.Type,
                    attribute.Length,
                    attribute.Precision,
                    attribute.Scale,
                    attribute.NotNull,
                    attribute.PrimaryKey,
                    attribute.Unique,
                    attribute.DefaultValue,
                    attribute.References))
                .ToArray();

            if (columns.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type '{modelType.FullName}' does not contain properties with DbColumnAttribute.");
            }

            return columns;
        }

        private static TableConstraint[] ReadConstraints(Type modelType)
        {
            return modelType
                .GetCustomAttributes<DbTableConstraintAttribute>()
                .Select(attribute => attribute.Type switch
                {
                    ConstraintType.PrimaryKey => string.IsNullOrWhiteSpace(attribute.Name)
                        ? TableConstraint.PrimaryKey(attribute.Columns)
                        : TableConstraint.NamedPrimaryKey(attribute.Name, attribute.Columns),
                    ConstraintType.Unique => string.IsNullOrWhiteSpace(attribute.Name)
                        ? TableConstraint.Unique(attribute.Columns)
                        : TableConstraint.NamedUnique(attribute.Name, attribute.Columns),
                    _ => throw new NotSupportedException($"Constraint type {attribute.Type} is not supported")
                })
                .ToArray();
        }

        private static IReadOnlyList<IndexDefinition> ReadIndexes(Type modelType)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            var indexes = modelType
                .GetCustomAttributes<DbIndexAttribute>()
                .Select(attribute => new IndexDefinition(attribute.Name, NormalizeIndexColumns(attribute.Columns), attribute.Unique))
                .ToList();

            foreach (var property in modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var column = property.GetCustomAttribute<DbColumnAttribute>();
                foreach (var attribute in property.GetCustomAttributes<DbIndexAttribute>())
                {
                    var columns = attribute.Columns.Length > 0
                        ? NormalizeIndexColumns(attribute.Columns)
                        : [column?.Name ?? property.Name];
                    indexes.Add(new IndexDefinition(attribute.Name, columns, attribute.Unique));
                }
            }

            return indexes;
        }

        private static IReadOnlyList<string> NormalizeIndexColumns(IReadOnlyCollection<string> columns)
        {
            if (columns.Count == 0)
            {
                throw new InvalidOperationException("Class-level DbIndexAttribute must contain at least one column.");
            }

            return columns
                .Select(column => string.IsNullOrWhiteSpace(column)
                    ? throw new InvalidOperationException("Index column name is empty.")
                    : column)
                .ToArray();
        }

        private sealed record IndexDefinition(string Name, IReadOnlyList<string> Columns, bool Unique);

        #endregion Attribute Mapping
    }
}