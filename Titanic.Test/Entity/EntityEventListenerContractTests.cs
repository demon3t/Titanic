using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Factory;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Entity.Attributes;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication.Configuration;
using Titanic.Test.Db;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тесты контрактов и базового pipeline событий Entity ORM.
    /// </summary>
    public sealed class EntityEventListenerContractTests : IClassFixture<DbManagerFixture>
    {
        #region Members

        private readonly Titanic.Db.Abstractions.BaseDbProvider _provider;
        private static TestEventSink? _currentSink;

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventListenerContractTests.
        /// </summary>
        public EntityEventListenerContractTests(DbManagerFixture fixture)
        {
            _provider = DbManager.GetProvider();
            global::Titanic.Entity.EntityManager.ResetServices();
            EmployeeEventListener.IsEnabled = false;
            DepartmentCancelEventListener.IsEnabled = false;
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventListenerAttribute_ShouldStoreEntityName.
        /// </summary>
        [Fact]
        public void EntityEventListenerAttribute_ShouldStoreEntityName()
        {
            var attribute = new EntityEventListenerAttribute("employees");

            Assert.Equal("employees", attribute.EntityName);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventArgs_Cancel_ShouldMarkPipelineAsCanceled.
        /// </summary>
        [Fact]
        public void EntityEventArgs_Cancel_ShouldMarkPipelineAsCanceled()
        {
            var args = new EntityEventArgs(
                new EntityDbManager(),
                EntityEventStage.Saving);

            args.Cancel("Validation failed.");

            Assert.True(args.IsCanceled);
            Assert.Equal("Validation failed.", args.CancelReason);
        }

        /// <summary>
        /// Инициализирует новый экземпляр BaseEntityEventListener_DefaultHandlers_ShouldBeNoOp.
        /// </summary>
        [Fact]
        public void BaseEntityEventListener_DefaultHandlers_ShouldBeNoOp()
        {
            var listener = new TestEntityEventListener();
            var entity = CreateEntity();

            listener.OnSaving(entity, CreateArgs(EntityEventStage.Saving));
            listener.OnSaved(entity, CreateArgs(EntityEventStage.Saved));
            listener.OnInserting(entity, CreateArgs(EntityEventStage.Inserting));
            listener.OnInserted(entity, CreateArgs(EntityEventStage.Inserted));
            listener.OnUpdating(entity, CreateArgs(EntityEventStage.Updating));
            listener.OnUpdated(entity, CreateArgs(EntityEventStage.Updated));
            listener.OnDeleting(entity, CreateArgs(EntityEventStage.Deleting));
            listener.OnDeleted(entity, CreateArgs(EntityEventStage.Deleted));
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_ShouldCallInsertEventPipeline.
        /// </summary>
        [Fact]
        public void Entity_Save_ShouldCallInsertEventPipeline()
        {
            var sink = new TestEventSink();
            ConfigureEntityServices(sink);
            var manager = CreateManager();
            var entity = manager.Create<OrmEmployeeEntity>(CreateUserConnection())
                .Set(nameof(OrmEmployeeEntity.Name), $"EVT-{Guid.NewGuid():N}")
                .Set(nameof(OrmEmployeeEntity.Email), $"{Guid.NewGuid():N}@mail.test")
                .Set(nameof(OrmEmployeeEntity.Salary), 100m)
                .Set(nameof(OrmEmployeeEntity.IsActive), true);

            Assert.True(entity.IsNew);

            entity.Save();

            Assert.Equal(
                new[] { "saving:employees", "inserting:employees", "inserted:employees", "saved:employees" },
                sink.Events);
            Assert.False(entity.IsNew);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_WithManualPrimaryKeyForNewRecord_ShouldUseInsertPipeline.
        /// </summary>
        [Fact]
        public void Entity_Save_WithManualPrimaryKeyForNewRecord_ShouldUseInsertPipeline()
        {
            var sink = new TestEventSink();
            ConfigureEntityServices(sink);
            var manager = CreateManager();
            var manualPrimaryKey = Random.Shared.Next(500_000, 2_000_000);
            var entity = manager.Create<OrmEmployeeEntity>(CreateUserConnection())
                .Set(nameof(OrmEmployeeEntity.Id), manualPrimaryKey)
                .Set(nameof(OrmEmployeeEntity.Name), $"EVT-PK-{Guid.NewGuid():N}")
                .Set(nameof(OrmEmployeeEntity.Email), $"{Guid.NewGuid():N}@mail.test")
                .Set(nameof(OrmEmployeeEntity.Salary), 200m)
                .Set(nameof(OrmEmployeeEntity.IsActive), true);

            Assert.True(entity.IsNew);

            entity.Save();

            Assert.Equal(
                new[] { "saving:employees", "inserting:employees", "inserted:employees", "saved:employees" },
                sink.Events);
            Assert.False(entity.IsNew);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_ShouldStopPipeline_WhenListenerCancelsOperation.
        /// </summary>
        [Fact]
        public void Entity_Save_ShouldStopPipeline_WhenListenerCancelsOperation()
        {
            var sink = new TestEventSink();
            ConfigureEntityServices(sink);
            var manager = CreateManager();
            var entity = manager.Create<OrmDepartmentEntity>(CreateUserConnection())
                .Set(nameof(OrmDepartmentEntity.Name), $"CANCEL-{Guid.NewGuid():N}");

            var exception = Assert.Throws<InvalidOperationException>(() => entity.Save());

            Assert.Equal("Department save canceled.", exception.Message);
            Assert.Equal(new[] { "saving:departments", "cancel:departments" }, sink.Events);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateEntity.
        /// </summary>
        private global::Titanic.Entity.Orm.Entity CreateEntity()
        {
            return global::Titanic.Entity.EntityManager.Create(
                typeof(OrmEmployeeEntity),
                _provider,
                CreateUserConnection());
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateManager.
        /// </summary>
        private BaseEntityManager CreateManager()
        {
            var manager = new EntityDbManager();
            manager.Initialize(
                "default",
                _provider,
                new EntityManagerSettings
                {
                    EntityModelNamespaces = new List<string> { typeof(OrmEmployeeEntity).Namespace! }
                });
            return manager;
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateArgs.
        /// </summary>
        private static EntityEventArgs CreateArgs(EntityEventStage stage)
        {
            return new EntityEventArgs(
                new EntityDbManager(),
                stage);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateUserConnection.
        /// </summary>
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
        /// Инициализирует новый экземпляр ConfigureEntityServices.
        /// </summary>
        private static void ConfigureEntityServices(TestEventSink sink)
        {
            var services = new ServiceCollection();
            _currentSink = sink;
            EmployeeEventListener.IsEnabled = true;
            DepartmentCancelEventListener.IsEnabled = true;

            var provider = services.BuildServiceProvider();
            global::Titanic.Entity.EntityManager.ConfigureServices(provider);
        }

        private sealed class TestEntityEventListener : BaseEntityEventListener
        {
        }

        [EntityEventListener("employees")]
        private sealed class EmployeeEventListener : BaseEntityEventListener
        {
            public static bool IsEnabled { get; set; }

            private readonly string _entityName;

            /// <summary>
            /// Инициализирует новый экземпляр EmployeeEventListener.
            /// </summary>
            public EmployeeEventListener(string entityName)
            {
                _entityName = entityName;
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnSaving.
            /// </summary>
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"saving:{_entityName}");
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnInserting.
            /// </summary>
            public override void OnInserting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"inserting:{_entityName}");
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnInserted.
            /// </summary>
            public override void OnInserted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"inserted:{_entityName}");
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnSaved.
            /// </summary>
            public override void OnSaved(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"saved:{_entityName}");
            }
        }

        [EntityEventListener("departments")]
        private sealed class DepartmentCancelEventListener : BaseEntityEventListener
        {
            public static bool IsEnabled { get; set; }

            private readonly string _entityName;

            /// <summary>
            /// Инициализирует новый экземпляр DepartmentCancelEventListener.
            /// </summary>
            public DepartmentCancelEventListener(string entityName)
            {
                _entityName = entityName;
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnSaving.
            /// </summary>
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"saving:{_entityName}");
                args.Cancel("Department save canceled.");
                _currentSink?.Events.Add($"cancel:{_entityName}");
            }
        }

        private sealed class TestEventSink
        {
            public List<string> Events { get; } = new();
        }

        #endregion Members
    }
}
