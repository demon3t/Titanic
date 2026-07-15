using System.Data;
using System.Data.Common;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Негенерик ORM builder SELECT-запросов; CLR-модель используется как metadata.
    /// </summary>
    public class EntitySelectBuilder
    {
        #region Members

        private const string RootAlias = "t0";
        private const string LocalizationColumnName = "SysCultureId";

        private readonly Dictionary<string, JoinState> _joins = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _localizationJoins = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _selectedPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SelectedColumnMetadata> _selectedColumns = new(StringComparer.OrdinalIgnoreCase);
        private readonly EntityStructure _rootStructure;
        private readonly EntityStructureScope _structureScope;
        private readonly BaseDbProvider _provider;
        private readonly BaseEntityManager? _manager;
        private QueryExpression? _whereExpression;
        private int _joinAliasIndex;

        public Select Select { get; }

        public UserConnection UserConnection { get; }

        internal Guid? LocalizationId { get; private set; }

        /// <summary>
        /// Инициализирует новый экземпляр EntitySelectBuilder.
        /// </summary>
        public EntitySelectBuilder(BaseDbProvider provider, Type entityType, UserConnection userConnection)
            : this(provider, Structure.DefaultScope, entityType, userConnection)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntitySelectBuilder.
        /// </summary>
        public EntitySelectBuilder(BaseDbProvider provider, string tableName, UserConnection userConnection)
            : this(provider, Structure.DefaultScope, tableName, userConnection)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntitySelectBuilder.
        /// </summary>
        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            Type entityType,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : this(provider, structureScope.GetEntityStructure(entityType), structureScope, userConnection, manager)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntitySelectBuilder.
        /// </summary>
        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            string tableName,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : this(provider, structureScope.GetEntityStructure(tableName), structureScope, userConnection, manager)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntitySelectBuilder.
        /// </summary>
        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructure rootStructure,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : this(provider, rootStructure, Structure.DefaultScope, userConnection, manager)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntitySelectBuilder.
        /// </summary>
        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructure rootStructure,
            EntityStructureScope structureScope,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(rootStructure);
            ArgumentNullException.ThrowIfNull(structureScope);
            ArgumentNullException.ThrowIfNull(userConnection);

            _provider = provider;
            _rootStructure = rootStructure;
            _structureScope = structureScope;
            _manager = manager;
            UserConnection = userConnection;
            UseLocalization(userConnection.Culture?.Id);
            Select = provider.Select();
            Select.SetFrom(_rootStructure.TableName, RootAlias);
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddColumn.
        /// </summary>
        public EntitySelectBuilder AddColumn(string path, string? alias = null)
        {
            var resolved = ResolvePath(path, allowTerminalReference: true);
            if (resolved.TerminalColumn.IsReference && resolved.TerminalEntity != null && resolved.ReferenceAlias != null)
            {
                AddReferenceColumn(path, alias, resolved);
            }
            else
            {
                AddSelectedColumn(
                    alias ?? path,
                    BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                    path,
                    resolved.TerminalColumn);
            }

            return this;
        }

        /// <summary>
        /// Добавить агрегатную колонку SELECT.
        /// </summary>
        /// <param name="path"> ORM-путь колонки или <c>*</c> для COUNT(*). </param>
        /// <param name="aggregationType"> Тип агрегатной функции. </param>
        /// <param name="alias"> Алиас результата. </param>
        /// <returns> Текущий builder. </returns>
        public EntitySelectBuilder AddAggregateColumn(
            string path,
            EntityAggregationType aggregationType,
            string? alias = null)
        {
            if (aggregationType == EntityAggregationType.None)
            {
                return AddColumn(path, alias);
            }

            var expression = BuildAggregateExpression(path, aggregationType, out var column);
            AddSelectedColumn(alias ?? CreateAggregateAlias(path, aggregationType), expression, path, column);
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddColumns.
        /// </summary>
        public EntitySelectBuilder AddColumns(params string[] paths)
        {
            foreach (var path in paths)
            {
                AddColumn(path);
            }

            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Distinct.
        /// </summary>
        public EntitySelectBuilder Distinct()
        {
            Select.Distinct();
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Where.
        /// </summary>
        public EntityWhereItem Where(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return new EntityWhereItem(
                this,
                BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                EntityWhereConnector.And);
        }

        /// <summary>
        /// Инициализирует новый экземпляр And.
        /// </summary>
        public EntityWhereItem And(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return new EntityWhereItem(
                this,
                BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                EntityWhereConnector.And);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Or.
        /// </summary>
        public EntityWhereItem Or(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return new EntityWhereItem(
                this,
                BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                EntityWhereConnector.Or);
        }

        /// <summary>
        /// Инициализирует новый экземпляр OrderBy.
        /// </summary>
        public EntitySelectBuilder OrderBy(string path, bool desc = false)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            Select.OrderBy(BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn), desc);
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр ThenBy.
        /// </summary>
        public EntitySelectBuilder ThenBy(string path, bool desc = false) => OrderBy(path, desc);

        /// <summary>
        /// Добавить группировку по ORM-пути.
        /// </summary>
        /// <param name="path"> ORM-путь колонки. </param>
        /// <returns> Текущий builder. </returns>
        public EntitySelectBuilder GroupBy(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            if (ShouldUseLocalization(resolved.CurrentEntity, resolved.TerminalColumn) && LocalizationId.HasValue)
            {
                var localizationAlias = EnsureLocalizationJoin(resolved.CurrentEntity, resolved.TerminalAlias);
                Select.GroupBy(localizationAlias, resolved.TerminalColumn.ColumnName);
                Select.GroupBy(resolved.TerminalAlias, resolved.TerminalColumn.ColumnName);
                return this;
            }

            Select.GroupBy(resolved.TerminalAlias, resolved.TerminalColumn.ColumnName);
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Take.
        /// </summary>
        public EntitySelectBuilder Take(int count)
        {
            Select.Limit(count).Take(count);
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Skip.
        /// </summary>
        public EntitySelectBuilder Skip(int count)
        {
            Select.Limit(int.MaxValue).Skip(count);
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Page.
        /// </summary>
        public EntitySelectBuilder Page(int page, int pageSize)
        {
            Select.Limit(pageSize).Page(page, pageSize);
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр UseLocalization.
        /// </summary>
        internal EntitySelectBuilder UseLocalization(Guid? localizationId)
        {
            LocalizationId = localizationId;
            return this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Build.
        /// </summary>
        public QueryBuildResult Build()
        {
            ApplyPendingWhere();
            return Select.Build();
        }

        /// <summary>
        /// Инициализирует новый экземпляр ToSql.
        /// </summary>
        public string ToSql()
        {
            ApplyPendingWhere();
            return Select.ToSql();
        }

        /// <summary>
        /// Инициализирует новый экземпляр ToRecords.
        /// </summary>
        public List<Entity> ToRecords()
        {
            ApplyPendingWhere();
            return Select.ExecuteReader(BuildRecord);
        }

        /// <summary>
        /// Инициализирует новый экземпляр FirstOrDefaultRecord.
        /// </summary>
        public Entity? FirstOrDefaultRecord() => ToRecords().FirstOrDefault();

        /// <summary>
        /// Инициализирует новый экземпляр CreateRecord.
        /// </summary>
        public Entity CreateRecord(IReadOnlyDictionary<string, object?> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            return new Entity(
                BuildColumnValues(values),
                new Dictionary<string, string>(_selectedPaths, StringComparer.OrdinalIgnoreCase),
                BuildAliasToColumnMap(),
                _rootStructure,
                _provider,
                UserConnection,
                isNew: false,
                manager: _manager);
        }

        /// <summary>
        /// Выполняет reader-запрос.
        /// </summary>
        public List<T> ExecuteReader<T>(Func<DbDataReader, T> mapRow)
        {
            ApplyPendingWhere();
            return Select.ExecuteReader(mapRow);
        }

        /// <summary>
        /// Выполняет reader-запрос.
        /// </summary>
        public List<T> ExecuteReader<T>(Func<IDataReader, T> mapRow)
        {
            ApplyPendingWhere();
            return Select.ExecuteReader(mapRow);
        }

        /// <summary>
        /// Создаёт Entity Schema Query.
        /// </summary>
        public List<T> Query<T>(Func<DbDataReader, T> mapRow)
        {
            ApplyPendingWhere();
            return Select.Query(mapRow);
        }

        /// <summary>
        /// Выполняет scalar-запрос.
        /// </summary>
        public T? ExecuteScalar<T>()
        {
            ApplyPendingWhere();
            return Select.ExecuteScalar<T>();
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddWhereExpression.
        /// </summary>
        internal EntitySelectBuilder AddWhereExpression(QueryExpression expression, EntityWhereConnector connector)
        {
            _whereExpression = _whereExpression == null
                ? expression
                : connector == EntityWhereConnector.Or
                    ? QueryExpression.Or(_whereExpression, expression)
                    : QueryExpression.And(_whereExpression, expression);

            return this;
        }

        /// <summary>
        /// Строит выражение чтения колонки, на которую указывает переданный путь сущности.
        /// </summary>
        /// <param name="path">Путь сущности к колонке, используемой в WHERE-выражении.</param>
        /// <returns>Выражение запроса для найденной колонки.</returns>
        internal QueryExpression BuildWhereColumnExpression(string path)
        {
            return BuildWhereColumnExpression(path, out _);
        }

        /// <summary>
        /// Строит выражение чтения колонки и возвращает ее метаданные для вызывающего кода,
        /// которому нужна нормализация значений с учетом типа.
        /// </summary>
        /// <param name="path">Путь сущности к колонке, используемой в WHERE-выражении.</param>
        /// <param name="column">Метаданные конечной колонки, найденной по переданному пути.</param>
        /// <returns>Выражение запроса для найденной колонки.</returns>
        internal QueryExpression BuildWhereColumnExpression(string path, out ColumnStructure column)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            column = resolved.TerminalColumn;
            return BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn);
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddMainColumns.
        /// </summary>
        private void AddMainColumns(EntityStructure structure, string tableAlias, string pathPrefix)
        {
            foreach (var column in structure.GetMainColumnStructure())
            {
                var path = $"{pathPrefix}.{column.PropertyName}";
                AddSelectedColumn(path, BuildReadExpression(structure, tableAlias, column), path, column);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddReferenceColumn.
        /// </summary>
        private void AddReferenceColumn(string path, string? alias, ResolvedPath resolved)
        {
            var key = alias ?? path;
            var valueAlias = CreateAliasPrefix(key);
            var displayAlias = $"{valueAlias}_DisplayValue";
            var displayColumn = resolved.TerminalEntity!.GetDisplayColumnStructure();

            Select.Column(BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn).As(valueAlias));
            Select.Column(BuildReadExpression(resolved.TerminalEntity, resolved.ReferenceAlias!, displayColumn).As(displayAlias));

            _selectedPaths[path] = valueAlias;
            _selectedColumns[valueAlias] = new SelectedColumnMetadata(resolved.TerminalColumn, displayAlias);
            _selectedColumns[displayAlias] = new SelectedColumnMetadata(displayColumn, DisplayAlias: null, IsHidden: true);
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddSelectedColumn.
        /// </summary>
        private void AddSelectedColumn(string key, QueryExpression expression, string sourcePath, ColumnStructure? column)
        {
            var alias = CreateAliasPrefix(key);
            Select.Column(expression.As(alias));
            _selectedPaths[sourcePath] = alias;
            _selectedColumns[alias] = new SelectedColumnMetadata(column, DisplayAlias: null);
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildRecord.
        /// </summary>
        private Entity BuildRecord(DbDataReader reader)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                values[name] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return CreateRecord(values);
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildColumnValues.
        /// </summary>
        private Dictionary<string, ColumnValue> BuildColumnValues(IReadOnlyDictionary<string, object?> values)
        {
            var result = new Dictionary<string, ColumnValue>(StringComparer.OrdinalIgnoreCase);
            var consumedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var selected in _selectedColumns)
            {
                var alias = selected.Key;
                var metadata = selected.Value;
                if (metadata.IsHidden)
                {
                    continue;
                }

                values.TryGetValue(alias, out var value);
                object? displayValue = null;
                if (metadata.DisplayAlias != null)
                {
                    values.TryGetValue(metadata.DisplayAlias, out displayValue);
                    consumedAliases.Add(metadata.DisplayAlias);
                }

                result[alias] = Entity.CreateColumnValue(alias, metadata.Column, value, displayValue);
                consumedAliases.Add(alias);
            }

            foreach (var value in values)
            {
                if (consumedAliases.Contains(value.Key))
                {
                    continue;
                }

                var column = _selectedColumns.TryGetValue(value.Key, out var metadata)
                    ? metadata.Column
                    : null;
                result[value.Key] = Entity.CreateColumnValue(value.Key, column, value.Value, displayValue: null);
            }

            return result;
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildAliasToColumnMap.
        /// </summary>
        private Dictionary<string, ColumnStructure> BuildAliasToColumnMap()
        {
            var result = _selectedColumns
                .Where(x => !x.Value.IsHidden && x.Value.Column != null)
                .ToDictionary(x => x.Key, x => x.Value.Column!, StringComparer.OrdinalIgnoreCase);

            foreach (var path in _selectedPaths)
            {
                if (result.TryGetValue(path.Value, out var column))
                {
                    result[path.Key] = column;
                }
            }

            return result;
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResolvePath.
        /// </summary>
        private ResolvedPath ResolvePath(string path, bool allowTerminalReference)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("ORM path is empty", nameof(path));
            }

            var parts = SplitPath(path);
            var currentEntity = _rootStructure;
            var currentAlias = RootAlias;
            var currentPath = string.Empty;

            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                var isLast = i == parts.Count - 1;

                if (IsReverseJoinPart(part))
                {
                    var descriptor = ParseReverseJoinDescriptor(part);
                    var join = EnsureReverseJoin(currentEntity, currentAlias, currentPath, descriptor);
                    currentEntity = join.Structure;
                    currentAlias = join.Alias;
                    currentPath = AppendPath(currentPath, part);

                    if (isLast)
                    {
                        return new ResolvedPath(
                            currentEntity,
                            currentAlias,
                            currentEntity.GetColumnStructure(descriptor.PrimaryColumnName),
                            TerminalEntity: null,
                            ReferenceAlias: null);
                    }

                    continue;
                }

                var column = currentEntity.GetColumnStructure(part);
                if (column.IsReference)
                {
                    var sourceEntity = currentEntity;
                    var sourceAlias = currentAlias;
                    var join = EnsureLeftJoin(currentEntity, currentAlias, currentPath, column);
                    currentEntity = join.Structure;
                    currentAlias = join.Alias;
                    currentPath = AppendPath(currentPath, part);

                    if (isLast)
                    {
                        return FinalizeResolvedPath(
                            sourceEntity,
                            sourceAlias,
                            currentEntity,
                            currentAlias,
                            column,
                            allowTerminalReference,
                            fromReference: true);
                    }

                    continue;
                }

                if (!isLast)
                {
                    throw new InvalidOperationException(
                        $"Path segment '{part}' in entity '{currentEntity.TableName}' is not a relation and cannot have child path.");
                }

                return new ResolvedPath(currentEntity, currentAlias, column, TerminalEntity: null, ReferenceAlias: null);
            }

            throw new InvalidOperationException($"ORM path '{path}' cannot be resolved.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр FinalizeResolvedPath.
        /// </summary>
        private ResolvedPath FinalizeResolvedPath(
            EntityStructure sourceEntity,
            string sourceAlias,
            EntityStructure targetEntity,
            string targetAlias,
            ColumnStructure referenceColumn,
            bool allowTerminalReference,
            bool fromReference)
        {
            if (allowTerminalReference && fromReference)
            {
                return new ResolvedPath(sourceEntity, sourceAlias, referenceColumn, targetEntity, targetAlias);
            }

            if (fromReference)
            {
                throw new InvalidOperationException(
                    $"Path must end with a concrete column. Use '{referenceColumn.PropertyName}.SomeColumn' or AddColumn('{referenceColumn.PropertyName}') for selecting relation with display value.");
            }

            return new ResolvedPath(targetEntity, targetAlias, targetEntity.GetColumnStructure(referenceColumn.PropertyName), null, null);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureLeftJoin.
        /// </summary>
        private JoinState EnsureLeftJoin(
            EntityStructure sourceEntity,
            string sourceAlias,
            string parentPath,
            ColumnStructure referenceColumn)
        {
            var pathKey = AppendPath(parentPath, referenceColumn.PropertyName);
            if (_joins.TryGetValue(pathKey, out var existing))
            {
                return existing;
            }

            if (string.IsNullOrWhiteSpace(referenceColumn.ReferenceTableName))
            {
                throw new InvalidOperationException(
                    $"Column '{referenceColumn.PropertyName}' in table '{sourceEntity.TableName}' is not configured as reference.");
            }

            var targetEntity = _structureScope.GetEntityStructure(referenceColumn.ReferenceTableName);
            var targetPrimaryColumn = targetEntity.GetPrimaryColumnStructure();
            var alias = NextJoinAlias();

            Select.LeftJoin(targetEntity.TableName)
                .As(alias)
                .On(sourceAlias, referenceColumn.ColumnName)
                .IsEqual(alias, targetPrimaryColumn.ColumnName);

            var join = new JoinState(alias, targetEntity);
            _joins[pathKey] = join;
            return join;
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureReverseJoin.
        /// </summary>
        private JoinState EnsureReverseJoin(
            EntityStructure sourceEntity,
            string sourceAlias,
            string parentPath,
            ReverseJoinDescriptor descriptor)
        {
            var pathKey = AppendPath(parentPath, descriptor.RawSegment);
            if (_joins.TryGetValue(pathKey, out var existing))
            {
                return existing;
            }

            var sourceColumn = sourceEntity.GetColumnStructure(descriptor.MainColumnName);
            var reverseRelation = _structureScope.FindReverseRelation(
                sourceEntity,
                descriptor.RelationColumnName,
                descriptor.PrimaryColumnName);
            var alias = NextJoinAlias();

            Select.RightJoin(reverseRelation.Entity.TableName)
                .As(alias)
                .On(alias, reverseRelation.RelationColumn.ColumnName)
                .IsEqual(sourceAlias, sourceColumn.ColumnName);

            var join = new JoinState(alias, reverseRelation.Entity);
            _joins[pathKey] = join;
            return join;
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildReadExpression.
        /// </summary>
        private QueryExpression BuildReadExpression(EntityStructure entity, string tableAlias, ColumnStructure column)
        {
            if (!ShouldUseLocalization(entity, column) || !LocalizationId.HasValue)
            {
                return Column.Name(tableAlias, column.ColumnName);
            }

            var localizationAlias = EnsureLocalizationJoin(entity, tableAlias);

            return Func.Coalesce(
                Func.Custom(
                    "NULLIF",
                    Column.Name(localizationAlias, column.ColumnName),
                    Column.Const(string.Empty)),
                Column.Name(tableAlias, column.ColumnName));
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildAggregateExpression.
        /// </summary>
        private QueryExpression BuildAggregateExpression(
            string path,
            EntityAggregationType aggregationType,
            out ColumnStructure? column)
        {
            QueryExpression argument;
            if (string.Equals(path, "*", StringComparison.OrdinalIgnoreCase))
            {
                argument = Column.Asterisk();
                column = null;
            }
            else
            {
                var resolved = ResolvePath(path, allowTerminalReference: false);
                argument = BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn);
                column = resolved.TerminalColumn;
            }

            return aggregationType switch
            {
                EntityAggregationType.Count => Func.Count(argument),
                EntityAggregationType.Sum => Func.Sum(argument),
                EntityAggregationType.Avg => Func.Avg(argument),
                EntityAggregationType.Min => Func.Min(argument),
                EntityAggregationType.Max => Func.Max(argument),
                _ => throw new NotSupportedException($"Aggregation '{aggregationType}' is not supported.")
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateAggregateAlias.
        /// </summary>
        private static string CreateAggregateAlias(string path, EntityAggregationType aggregationType)
        {
            var normalizedPath = string.Equals(path, "*", StringComparison.OrdinalIgnoreCase)
                ? "All"
                : path;
            return $"{aggregationType}_{normalizedPath}";
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureLocalizationJoin.
        /// </summary>
        private string EnsureLocalizationJoin(EntityStructure entity, string tableAlias)
        {
            var key = $"{tableAlias}:{entity.TableName}";
            if (_localizationJoins.TryGetValue(key, out var existingAlias))
            {
                return existingAlias;
            }

            var alias = NextJoinAlias();
            var primaryColumn = entity.GetPrimaryColumnStructure();
            var recordCondition = QueryExpression.Binary(
                Column.Name(alias, "RecordId"),
                ConditionOperator.Equal,
                Column.Name(tableAlias, primaryColumn.ColumnName));

            var joinCondition = LocalizationId.HasValue
                ? QueryExpression.And(
                    recordCondition,
                    QueryExpression.Binary(
                        Column.Name(alias, LocalizationColumnName),
                        ConditionOperator.Equal,
                        Column.Parameter(LocalizationId.Value)))
                : recordCondition;

            Select.AddJoin(GetLocalizationTableName(entity.TableName), joinCondition, alias, JoinType.Left);
            _localizationJoins[key] = alias;
            return alias;
        }

        /// <summary>
        /// Инициализирует новый экземпляр ApplyPendingWhere.
        /// </summary>
        private void ApplyPendingWhere()
        {
            if (_whereExpression != null)
            {
                Select.Where(_whereExpression);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр NextJoinAlias.
        /// </summary>
        private string NextJoinAlias() => $"t{++_joinAliasIndex}";

        /// <summary>
        /// Инициализирует новый экземпляр SplitPath.
        /// </summary>
        private static List<string> SplitPath(string path)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            var bracketDepth = 0;

            foreach (var ch in path)
            {
                if (ch == '[')
                {
                    bracketDepth++;
                    current.Append(ch);
                    continue;
                }

                if (ch == ']')
                {
                    bracketDepth--;
                    current.Append(ch);
                    continue;
                }

                if (ch == '.' && bracketDepth == 0)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        /// <summary>
        /// Инициализирует новый экземпляр IsReverseJoinPart.
        /// </summary>
        private static bool IsReverseJoinPart(string part)
            => part.StartsWith('[') && part.EndsWith(']');

        /// <summary>
        /// Инициализирует новый экземпляр ParseReverseJoinDescriptor.
        /// </summary>
        private static ReverseJoinDescriptor ParseReverseJoinDescriptor(string part)
        {
            var body = part[1..^1];
            var pieces = body.Split(':', StringSplitOptions.TrimEntries);
            if (pieces.Length != 3 || pieces.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    $"Reverse join segment '{part}' must use format [RelationColumn:RelatedPrimaryColumn:MainColumn].",
                    nameof(part));
            }

            return new ReverseJoinDescriptor(part, pieces[0], pieces[1], pieces[2]);
        }

        /// <summary>
        /// Инициализирует новый экземпляр AppendPath.
        /// </summary>
        private static string AppendPath(string currentPath, string part)
            => string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}.{part}";

        /// <summary>
        /// Инициализирует новый экземпляр CreateAliasPrefix.
        /// </summary>
        private static string CreateAliasPrefix(string path)
        {
            var chars = path.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars).Trim('_');
        }

        /// <summary>
        /// Инициализирует новый экземпляр GetLocalizationTableName.
        /// </summary>
        private static string GetLocalizationTableName(string tableName)
        {
            var lastDotIndex = tableName.LastIndexOf('.');
            var name = lastDotIndex < 0 ? tableName : tableName[(lastDotIndex + 1)..];
            return $"sys_{name}_lcz";
        }

        /// <summary>
        /// Инициализирует новый экземпляр ShouldUseLocalization.
        /// </summary>
        private static bool ShouldUseLocalization(EntityStructure entity, ColumnStructure column)
        {
            return column.IsLocalized
                && !column.IsLocalizationDisabled
                && !entity.IsLocalizationDisabled;
        }

        private sealed record JoinState(string Alias, EntityStructure Structure);

        private sealed record ReverseJoinDescriptor(
            string RawSegment,
            string RelationColumnName,
            string PrimaryColumnName,
            string MainColumnName);

        private sealed record ResolvedPath(
            EntityStructure CurrentEntity,
            string TerminalAlias,
            ColumnStructure TerminalColumn,
            EntityStructure? TerminalEntity,
            string? ReferenceAlias);

        private sealed record SelectedColumnMetadata(
            ColumnStructure? Column,
            string? DisplayAlias,
            bool IsHidden = false);

        #endregion Members
    }
}
