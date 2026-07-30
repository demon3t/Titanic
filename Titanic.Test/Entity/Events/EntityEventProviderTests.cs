using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Session;
using Titanic.Db.PosgreSql;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Test.Entity;

/// <summary>
/// Тесты provider-расширения событийного слоя Entity ORM.
/// </summary>
public sealed class EntityEventProviderTests
{
    #region Members

    /// <summary>
    /// Проверяет, что новый provider можно подключить через DI без изменения dispatcher-а.
    /// </summary>
    [Fact]
    public void Entity_Save_WithCustomEventProvider_ShouldUseProviderFromDi()
    {
        EntityEventInMemoryDbProvider.ResetState();
        CustomEntityEventProvider.Reset();
        global::Titanic.Entity.EntityManager.Reset();
        global::Titanic.Entity.EntityManager.ResetServices();

        var services = new ServiceCollection();
        services.AddSingleton<BaseEntityEventProvider, CustomEntityEventProvider>();
        global::Titanic.Entity.EntityManager.ConfigureServices(services.BuildServiceProvider());

        var manager = CreateManager("custom://listener");
        var entity = manager.Create<OrmDepartmentEntity>(CreateUserConnection())
            .Set(nameof(OrmDepartmentEntity.Name), $"Provider-{Guid.NewGuid():N}");

        entity.Save();

        Assert.Equal(
            new[] { "Saving", "Inserting", "Inserted", "Saved" },
            CustomEntityEventProvider.Stages);
    }

    /// <summary>
    /// Создаёт тестовый менеджер с указанным внешним listener locator-ом.
    /// </summary>
    /// <param name="eventListener">Locator событийного listener-а.</param>
    /// <returns>Тестовый Entity ORM менеджер.</returns>
    private static BaseEntityManager CreateManager(string eventListener)
    {
        var manager = new EntityDbManager();
        manager.Initialize(
            "custom-provider",
            new EntityEventInMemoryDbProvider("in-memory", new PostgresEngine()),
            new EntityManagerSettings
            {
                EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                EventListener = eventListener
            });

        return manager;
    }

    /// <summary>
    /// Создаёт тестовый пользовательский контекст.
    /// </summary>
    /// <returns>Пользовательский контекст для Entity ORM.</returns>
    private static UserConnection CreateUserConnection()
    {
        return new UserConnection
        {
            UserId = Guid.NewGuid(),
            Culture = new UserCulture
            {
                Id = Guid.NewGuid(),
                Name = "Test"
            }
        };
    }

    /// <summary>
    /// Тестовый provider событийного слоя с собственной transport-схемой.
    /// </summary>
    private sealed class CustomEntityEventProvider : BaseEntityEventProvider
    {
        private static readonly object SyncRoot = new();
        private static readonly List<string> InternalStages = [];

        /// <summary>
        /// Стадии событийного pipeline, полученные provider-ом.
        /// </summary>
        public static IReadOnlyList<string> Stages
        {
            get
            {
                lock (SyncRoot)
                {
                    return InternalStages.ToArray();
                }
            }
        }

        /// <summary>
        /// Сбрасывает состояние тестового provider-а.
        /// </summary>
        public static void Reset()
        {
            lock (SyncRoot)
            {
                InternalStages.Clear();
            }
        }

        /// <inheritdoc />
        public override bool CanDispatch(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            return manager.EventListener?.StartsWith("custom://", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <inheritdoc />
        public override void Dispatch(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage,
            IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(services);

            lock (SyncRoot)
            {
                InternalStages.Add(stage.ToString());
            }
        }
    }

    #endregion Members
}
