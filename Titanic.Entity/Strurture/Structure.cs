using System.Reflection;
using Titanic.Entity.Attributes;
using Titanic.Entity.Exceptions;

namespace Titanic.Entity.Strurture
{
    /// <summary>
    /// Кэш структуры ORM-сущностей.
    /// </summary>
    public static class Structure
    {
        /// <summary>
        /// Структура сущностей.
        /// </summary>
        internal static List<EntityStructure> EntitiesStructure { get; private set; }

        /// <summary>
        /// Статический конструктор.
        /// </summary>
        static Structure()
        {
            EntitiesStructure = InitializeEntitiesStructure();
        }

        /// <summary>
        /// Получить структуру сущности по названию таблицы.
        /// </summary>
        internal static EntityStructure GetEntityStructure(string tableName)
        {
            var entityStructure = EntitiesStructure.FirstOrDefault(x =>
                string.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase));

            return entityStructure ?? throw new NotExistTableException(tableName);
        }

        /// <summary>
        /// Получить структуру сущности по CLR-типу, включая абстрактные модели.
        /// </summary>
        internal static EntityStructure GetEntityStructure(Type entityType)
        {
            ArgumentNullException.ThrowIfNull(entityType);

            var entityStructure = EntitiesStructure.FirstOrDefault(x => x.EntityType == entityType);
            if (entityStructure != null)
            {
                return entityStructure;
            }

            var entityAttr = entityType.GetCustomAttribute<EntityAttribute>();

            return entityAttr is null
                ? throw new NotExistTableException(entityType.Name)
                : GetEntityStructure(entityAttr.Table);
        }

        /// <summary>
        /// Получить структуру сущности по имени CLR-типа.
        /// </summary>
        internal static EntityStructure GetEntityStructureByTypeName(string entityTypeName)
        {
            var candidates = EntitiesStructure
                .Where(x =>
                    string.Equals(x.EntityType.FullName, entityTypeName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.EntityType.Name, entityTypeName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count == 0)
            {
                throw new NotExistTableException(entityTypeName);
            }

            if (candidates.Count > 1)
            {
                var names = string.Join(", ", candidates.Select(x => x.EntityType.FullName));
                throw new InvalidOperationException($"Entity type name '{entityTypeName}' is ambiguous: {names}");
            }

            return candidates[0];
        }

        /// <summary>
        /// Получить структуру сущности по CLR-типу.
        /// </summary>
        internal static EntityStructure GetEntityStructure<TEntity>()
        {
            return GetEntityStructure(typeof(TEntity));
        }

        /// <summary>
        /// Найти сущность для обратной связи по описанию RIGHT JOIN пути.
        /// </summary>
        internal static (EntityStructure Entity, ColumnStructure RelationColumn, ColumnStructure PrimaryColumn) FindReverseRelation(
            EntityStructure sourceEntity,
            string relationColumnName,
            string relatedPrimaryColumnName)
        {
            var candidates = EntitiesStructure
                .Select(entity => (
                    Entity: entity,
                    RelationColumn: entity.ColumnsStructure.FirstOrDefault(column =>
                        column.IsReference
                        && column.Matches(relationColumnName)
                        && string.Equals(column.ReferenceTableName, sourceEntity.TableName, StringComparison.OrdinalIgnoreCase)),
                    PrimaryColumn: entity.ColumnsStructure.FirstOrDefault(column =>
                        column.IsPrimary
                        && column.Matches(relatedPrimaryColumnName))))
                .Where(candidate => candidate.RelationColumn != null && candidate.PrimaryColumn != null)
                .ToList();

            if (candidates.Count == 0)
            {
                throw new NotExistTableException(
                    $"Reverse relation '{relationColumnName}:{relatedPrimaryColumnName}' for table '{sourceEntity.TableName}' not found");
            }

            if (candidates.Count > 1)
            {
                var names = string.Join(", ", candidates.Select(x => x.Entity.TableName));
                throw new InvalidOperationException(
                    $"Reverse relation '{relationColumnName}:{relatedPrimaryColumnName}' for table '{sourceEntity.TableName}' is ambiguous: {names}");
            }

            var candidate = candidates[0];
            return (candidate.Entity, candidate.RelationColumn!, candidate.PrimaryColumn!);
        }

        /// <summary>
        /// Пересканировать загруженные сборки и пересобрать структуру.
        /// </summary>
        internal static void Reload()
        {
            EntitiesStructure = InitializeEntitiesStructure();
        }

        private static List<EntityStructure> InitializeEntitiesStructure()
        {
            var result = new List<EntityStructure>();

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetLoadableTypes);

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

            return result;
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
