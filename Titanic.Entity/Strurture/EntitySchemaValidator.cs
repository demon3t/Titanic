using Titanic.Db;
using Titanic.Db.Abstractions;

namespace Titanic.Entity.Strurture
{
    /// <summary>
    /// Проверяет соответствие Entity-моделей фактической структуре БД.
    /// </summary>
    internal static class EntitySchemaValidator
    {
        #region Constants

        private const string DefaultSchemaName = "public";
        private const string LocalizationCultureColumnName = "SysCultureId";
        private const string LocalizationRecordColumnName = "RecordId";

        #endregion Constants

        #region Public Methods

        /// <summary>
        /// Проверить наличие таблиц и колонок, описанных Entity-моделями.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        public static void Validate(BaseDbProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            var entities = Structure.EntitiesStructure
                .GroupBy(entity => entity.TableName, StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    TableName = group.Key,
                    Columns = group
                        .SelectMany(entity => entity.ColumnsStructure)
                        .GroupBy(column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
                        .Select(column => column.First())
                        .ToList(),
                    LocalizedColumns = group
                        .Where(entity => !entity.IsLocalizationDisabled)
                        .SelectMany(entity => entity.ColumnsStructure)
                        .Where(column => column.IsLocalized && !column.IsLocalizationDisabled)
                        .GroupBy(column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
                        .Select(column => column.First())
                        .ToList()
                })
                .ToList();

            foreach (var entity in entities)
            {
                ValidateTable(provider, entity.TableName, entity.Columns.Select(column => column.ColumnName));

                if (entity.LocalizedColumns.Count > 0)
                {
                    ValidateLocalizationTable(
                        provider,
                        entity.TableName,
                        entity.LocalizedColumns.Select(column => column.ColumnName));
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Проверить таблицу и обязательные колонки.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <param name="columnNames"> Имена колонок. </param>
        private static void ValidateTable(BaseDbProvider provider, string tableName, IEnumerable<string> columnNames)
        {
            var table = ParseTableName(tableName);
            var actualColumns = LoadTableColumns(provider, table.SchemaName, table.TableName);

            if (actualColumns.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Entity schema validation failed: table '{tableName}' not found in database.");
            }

            foreach (var columnName in columnNames)
            {
                if (!actualColumns.Contains(columnName))
                {
                    throw new InvalidOperationException(
                        $"Entity schema validation failed: column '{columnName}' not found in table '{tableName}'.");
                }
            }
        }

        /// <summary>
        /// Проверить таблицу локализации для локализуемых колонок.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="baseTableName"> Имя основной таблицы. </param>
        /// <param name="localizedColumnNames"> Локализуемые колонки. </param>
        private static void ValidateLocalizationTable(
            BaseDbProvider provider,
            string baseTableName,
            IEnumerable<string> localizedColumnNames)
        {
            var localizationTableName = GetLocalizationTableName(baseTableName);
            var requiredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                LocalizationRecordColumnName,
                LocalizationCultureColumnName
            };

            foreach (var columnName in localizedColumnNames)
            {
                requiredColumns.Add(columnName);
            }

            ValidateTable(provider, localizationTableName, requiredColumns);
        }

        /// <summary>
        /// Загрузить имена колонок таблицы из information_schema.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="schemaName"> Имя схемы. </param>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <returns> Имена колонок. </returns>
        private static HashSet<string> LoadTableColumns(BaseDbProvider provider, string schemaName, string tableName)
        {
            var query = provider.Select()
                .Column("c", "column_name")
                .From("information_schema.columns").As("c")
                .Where("c", "table_schema").IsEqual(Column.Parameter(schemaName))
                .Where("c", "table_name").IsEqual(Column.Parameter(tableName));

            return provider
                .ExecuteReader(query, reader => reader.GetString(0))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Получить имя таблицы локализации по имени основной таблицы.
        /// </summary>
        /// <param name="tableName"> Имя основной таблицы. </param>
        /// <returns> Имя таблицы локализации. </returns>
        private static string GetLocalizationTableName(string tableName)
        {
            var table = ParseTableName(tableName);
            return $"sys_{table.TableName}_lcz";
        }

        /// <summary>
        /// Разобрать имя таблицы на схему и имя.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <returns> Схема и имя таблицы. </returns>
        private static (string SchemaName, string TableName) ParseTableName(string tableName)
        {
            var parts = tableName.Split('.', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2
                ? (parts[0], parts[1])
                : (DefaultSchemaName, tableName);
        }

        #endregion Private Methods
    }
}
