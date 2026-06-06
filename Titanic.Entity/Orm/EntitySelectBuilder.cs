using System.Data;
using System.Data.Common;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Non-generic ORM SELECT builder. The CLR model is metadata only.
    /// </summary>
    public class EntitySelectBuilder
    {
        private const string RootAlias = "t0";
        private const string LocalizationColumnName = "SysCultureId";

        private readonly Dictionary<string, JoinState> _joins = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _localizationJoins = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _selectedPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SelectedColumnMetadata> _selectedColumns = new(StringComparer.OrdinalIgnoreCase);
        private readonly EntityStructure _rootStructure;
        private readonly EntityStructureScope _structureScope;
        private readonly BaseDbProvider _provider;
        private QueryExpression? _whereExpression;
        private int _joinAliasIndex;

        public Select Select { get; }

        public UserConnection UserConnection { get; }

        internal Guid? LocalizationId { get; private set; }

        public EntitySelectBuilder(BaseDbProvider provider, Type entityType, UserConnection userConnection)
            : this(provider, Structure.DefaultScope, entityType, userConnection)
        {
        }

        public EntitySelectBuilder(BaseDbProvider provider, string tableName, UserConnection userConnection)
            : this(provider, Structure.DefaultScope, tableName, userConnection)
        {
        }

        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            Type entityType,
            UserConnection userConnection)
            : this(provider, structureScope.GetEntityStructure(entityType), structureScope, userConnection)
        {
        }

        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            string tableName,
            UserConnection userConnection)
            : this(provider, structureScope.GetEntityStructure(tableName), structureScope, userConnection)
        {
        }

        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructure rootStructure,
            UserConnection userConnection)
            : this(provider, rootStructure, Structure.DefaultScope, userConnection)
        {
        }

        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructure rootStructure,
            EntityStructureScope structureScope,
            UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(rootStructure);
            ArgumentNullException.ThrowIfNull(structureScope);
            ArgumentNullException.ThrowIfNull(userConnection);

            _provider = provider;
            _rootStructure = rootStructure;
            _structureScope = structureScope;
            UserConnection = userConnection;
            UseLocalization(userConnection.Culture?.Id);
            Select = provider.Select();
            Select.SetFrom(_rootStructure.TableName, RootAlias);
        }

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

        public EntitySelectBuilder AddColumns(params string[] paths)
        {
            foreach (var path in paths)
            {
                AddColumn(path);
            }

            return this;
        }

        public EntitySelectBuilder Distinct()
        {
            Select.Distinct();
            return this;
        }

        public EntityWhereItem Where(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return new EntityWhereItem(
                this,
                BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                EntityWhereConnector.And);
        }

        public EntityWhereItem And(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return new EntityWhereItem(
                this,
                BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                EntityWhereConnector.And);
        }

        public EntityWhereItem Or(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return new EntityWhereItem(
                this,
                BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn),
                EntityWhereConnector.Or);
        }

        public EntitySelectBuilder OrderBy(string path, bool desc = false)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            Select.OrderBy(BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn), desc);
            return this;
        }

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

        public EntitySelectBuilder Take(int count)
        {
            Select.Limit(count).Take(count);
            return this;
        }

        public EntitySelectBuilder Skip(int count)
        {
            Select.Limit(int.MaxValue).Skip(count);
            return this;
        }

        public EntitySelectBuilder Page(int page, int pageSize)
        {
            Select.Limit(pageSize).Page(page, pageSize);
            return this;
        }

        internal EntitySelectBuilder UseLocalization(Guid? localizationId)
        {
            LocalizationId = localizationId;
            return this;
        }

        public QueryBuildResult Build()
        {
            ApplyPendingWhere();
            return Select.Build();
        }

        public string ToSql()
        {
            ApplyPendingWhere();
            return Select.ToSql();
        }

        public List<Entity> ToRecords()
        {
            ApplyPendingWhere();
            return Select.ExecuteReader(BuildRecord);
        }

        public Entity? FirstOrDefaultRecord() => ToRecords().FirstOrDefault();

        public Entity CreateRecord(IReadOnlyDictionary<string, object?> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            return new Entity(
                BuildColumnValues(values),
                new Dictionary<string, string>(_selectedPaths, StringComparer.OrdinalIgnoreCase),
                BuildAliasToColumnMap(),
                _rootStructure,
                _provider,
                UserConnection);
        }

        public List<T> ExecuteReader<T>(Func<DbDataReader, T> mapRow)
        {
            ApplyPendingWhere();
            return Select.ExecuteReader(mapRow);
        }

        public List<T> ExecuteReader<T>(Func<IDataReader, T> mapRow)
        {
            ApplyPendingWhere();
            return Select.ExecuteReader(mapRow);
        }

        public List<T> Query<T>(Func<DbDataReader, T> mapRow)
        {
            ApplyPendingWhere();
            return Select.Query(mapRow);
        }

        public T? ExecuteScalar<T>()
        {
            ApplyPendingWhere();
            return Select.ExecuteScalar<T>();
        }

        internal EntitySelectBuilder AddWhereExpression(QueryExpression expression, EntityWhereConnector connector)
        {
            _whereExpression = _whereExpression == null
                ? expression
                : connector == EntityWhereConnector.Or
                    ? QueryExpression.Or(_whereExpression, expression)
                    : QueryExpression.And(_whereExpression, expression);

            return this;
        }

        internal QueryExpression BuildWhereColumnExpression(string path)
        {
            var resolved = ResolvePath(path, allowTerminalReference: false);
            return BuildReadExpression(resolved.CurrentEntity, resolved.TerminalAlias, resolved.TerminalColumn);
        }

        private void AddMainColumns(EntityStructure structure, string tableAlias, string pathPrefix)
        {
            foreach (var column in structure.GetMainColumnStructure())
            {
                var path = $"{pathPrefix}.{column.PropertyName}";
                AddSelectedColumn(path, BuildReadExpression(structure, tableAlias, column), path, column);
            }
        }

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

        private void AddSelectedColumn(string key, QueryExpression expression, string sourcePath, ColumnStructure? column)
        {
            var alias = CreateAliasPrefix(key);
            Select.Column(expression.As(alias));
            _selectedPaths[sourcePath] = alias;
            _selectedColumns[alias] = new SelectedColumnMetadata(column, DisplayAlias: null);
        }

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

        private static string CreateAggregateAlias(string path, EntityAggregationType aggregationType)
        {
            var normalizedPath = string.Equals(path, "*", StringComparison.OrdinalIgnoreCase)
                ? "All"
                : path;
            return $"{aggregationType}_{normalizedPath}";
        }

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

        private void ApplyPendingWhere()
        {
            if (_whereExpression != null)
            {
                Select.Where(_whereExpression);
            }
        }

        private string NextJoinAlias() => $"t{++_joinAliasIndex}";

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

        private static bool IsReverseJoinPart(string part)
            => part.StartsWith('[') && part.EndsWith(']');

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

        private static string AppendPath(string currentPath, string part)
            => string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}.{part}";

        private static string CreateAliasPrefix(string path)
        {
            var chars = path.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars).Trim('_');
        }

        private static string GetLocalizationTableName(string tableName)
        {
            var lastDotIndex = tableName.LastIndexOf('.');
            var name = lastDotIndex < 0 ? tableName : tableName[(lastDotIndex + 1)..];
            return $"sys_{name}_lcz";
        }

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
    }
}
