using System.Reflection;
using System.Text.RegularExpressions;
using Titanic.Entity.Attributes;

namespace Titanic.Entity.Strurture
{
    /// <summary>
    /// Кэш структур ORM-сущностей и фабрика manager-specific scope.
    /// </summary>
    public static class Structure
    {
        private static readonly object SyncRoot = new();
        private static readonly Dictionary<string, EntityStructureScope> ScopedStructures = new(StringComparer.Ordinal);
        private static EntityStructureScope _defaultScope = BuildScope([]);

        /// <summary>
        /// Глобальная структура без фильтрации по namespace.
        /// </summary>
        internal static EntityStructureScope DefaultScope => _defaultScope;

        /// <summary>
        /// Получить структуру сущности по названию таблицы.
        /// </summary>
        internal static EntityStructure GetEntityStructure(string tableName)
        {
            return DefaultScope.GetEntityStructure(tableName);
        }

        /// <summary>
        /// Получить структуру сущности по CLR-типу.
        /// </summary>
        internal static EntityStructure GetEntityStructure(Type entityType)
        {
            return DefaultScope.GetEntityStructure(entityType);
        }

        /// <summary>
        /// Получить структуру сущности по имени CLR-типа.
        /// </summary>
        internal static EntityStructure GetEntityStructureByTypeName(string entityTypeName)
        {
            return DefaultScope.GetEntityStructureByTypeName(entityTypeName);
        }

        /// <summary>
        /// Получить структуру сущности по CLR-типу.
        /// </summary>
        internal static EntityStructure GetEntityStructure<TEntity>()
        {
            return DefaultScope.GetEntityStructure(typeof(TEntity));
        }

        /// <summary>
        /// Найти сущность для обратной связи по описанию RIGHT JOIN пути.
        /// </summary>
        internal static (EntityStructure Entity, ColumnStructure RelationColumn, ColumnStructure PrimaryColumn) FindReverseRelation(
            EntityStructure sourceEntity,
            string relationColumnName,
            string relatedPrimaryColumnName)
        {
            return DefaultScope.FindReverseRelation(sourceEntity, relationColumnName, relatedPrimaryColumnName);
        }

        /// <summary>
        /// Получить manager-specific scope структуры по namespace-patterns.
        /// </summary>
        internal static EntityStructureScope GetScope(IEnumerable<string>? namespacePatterns)
        {
            var normalizedPatterns = NormalizeNamespacePatterns(namespacePatterns);
            if (normalizedPatterns.Count == 0)
            {
                return DefaultScope;
            }

            var key = string.Join('|', normalizedPatterns);
            lock (SyncRoot)
            {
                if (!ScopedStructures.TryGetValue(key, out var scope))
                {
                    scope = BuildScope(normalizedPatterns);
                    ScopedStructures[key] = scope;
                }

                return scope;
            }
        }

        /// <summary>
        /// Пересканировать загруженные сборки и пересобрать структуру.
        /// </summary>
        internal static void Reload()
        {
            lock (SyncRoot)
            {
                _defaultScope = BuildScope([]);
                ScopedStructures.Clear();
            }
        }

        private static EntityStructureScope BuildScope(IReadOnlyList<string> namespacePatterns)
        {
            var result = new List<EntityStructure>();

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => MatchesNamespacePatterns(type, namespacePatterns));

            foreach (var type in types)
            {
                var entityAttr = type.GetCustomAttribute<EntityAttribute>();
                if (entityAttr == null)
                {
                    continue;
                }

                var entityStructure = new EntityStructure
                {
                    EntityType = type,
                    TableName = entityAttr.Table,
                    IsView = entityAttr.IsView,
                    IsLocalizationDisabled = type.IsDefined(typeof(DisableLocalizationAttribute)),
                    ColumnsStructure = []
                };

                foreach (var prop in type.GetProperties())
                {
                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    if (columnAttr == null)
                    {
                        continue;
                    }

                    var isPrimary = prop.IsDefined(typeof(PrimaryColumnAttribute));
                    var isDisplay = prop.IsDefined(typeof(DisplayColumnAttribute));
                    var isReference = prop.IsDefined(typeof(ReferenceColumnAttribute));
                    var referenceAttr = prop.GetCustomAttribute<ReferenceColumnAttribute>();

                    entityStructure.ColumnsStructure.Add(new ColumnStructure
                    {
                        PropertyName = prop.Name,
                        ColumnName = columnAttr.Column,
                        DataValueType = columnAttr.DataValueType,
                        IsNullable = !isPrimary || !isDisplay || isReference,
                        IsPrimary = isPrimary,
                        IsDisplay = isDisplay,
                        IsLocalized = columnAttr.IsLocalized,
                        IsLocalizationDisabled = prop.IsDefined(typeof(DisableLocalizationAttribute)),
                        ReferenceTableName = referenceAttr?.Table
                    });
                }

                result.Add(entityStructure);
            }

            return new EntityStructureScope
            {
                NamespacePatterns = namespacePatterns,
                EntitiesStructure = result
            };
        }

        private static IReadOnlyList<string> NormalizeNamespacePatterns(IEnumerable<string>? namespacePatterns)
        {
            return namespacePatterns?
                .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                .Select(pattern => pattern.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(pattern => pattern, StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? [];
        }

        private static bool MatchesNamespacePatterns(Type type, IReadOnlyList<string> namespacePatterns)
        {
            if (type.GetCustomAttribute<EntityAttribute>() == null)
            {
                return false;
            }

            if (namespacePatterns.Count == 0)
            {
                return true;
            }

            var typeNamespace = type.Namespace ?? string.Empty;
            return namespacePatterns.Any(pattern => MatchesNamespacePattern(typeNamespace, pattern));
        }

        private static bool MatchesNamespacePattern(string typeNamespace, string pattern)
        {
            if (pattern.EndsWith(".*", StringComparison.Ordinal))
            {
                var prefix = pattern[..^2];
                return string.Equals(typeNamespace, prefix, StringComparison.OrdinalIgnoreCase)
                    || typeNamespace.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);
            }

            if (!pattern.Contains('*'))
            {
                return string.Equals(typeNamespace, pattern, StringComparison.OrdinalIgnoreCase);
            }

            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(typeNamespace, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null)!;
            }
        }
    }
}
