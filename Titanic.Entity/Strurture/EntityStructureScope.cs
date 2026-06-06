using System.Reflection;
using Titanic.Entity.Attributes;
using Titanic.Entity.Exceptions;

namespace Titanic.Entity.Strurture
{
    /// <summary>
    /// Область видимости структуры Entity ORM для конкретного менеджера.
    /// </summary>
    internal sealed class EntityStructureScope
    {
        /// <summary>
        /// Коллекция namespace-patterns, по которым собиралась структура.
        /// Пустая коллекция означает глобальную структуру без фильтрации.
        /// </summary>
        public IReadOnlyList<string> NamespacePatterns { get; init; } = [];

        /// <summary>
        /// Структуры сущностей, доступные в этой области видимости.
        /// </summary>
        public IReadOnlyList<EntityStructure> EntitiesStructure { get; init; } = [];

        /// <summary>
        /// Получить структуру сущности по имени таблицы.
        /// </summary>
        public EntityStructure GetEntityStructure(string tableName)
        {
            var entityStructure = EntitiesStructure.FirstOrDefault(x =>
                string.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase));

            return entityStructure ?? throw new NotExistTableException(tableName);
        }

        /// <summary>
        /// Получить структуру сущности по CLR-типу.
        /// </summary>
        public EntityStructure GetEntityStructure(Type entityType)
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
        public EntityStructure GetEntityStructureByTypeName(string entityTypeName)
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
        /// Найти сущность для обратной связи по описанию RIGHT JOIN пути.
        /// </summary>
        public (EntityStructure Entity, ColumnStructure RelationColumn, ColumnStructure PrimaryColumn) FindReverseRelation(
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
    }
}
