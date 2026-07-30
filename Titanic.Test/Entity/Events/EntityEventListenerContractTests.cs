using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Interfaces;
using Titanic.Common.Session;
using Titanic.Db.PosgreSql;
using Titanic.Entity.Attributes;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тесты контрактов и базового pipeline событий Entity ORM.
    /// </summary>
    public sealed class EntityEventListenerContractTests
    {
        #region Members

        private readonly EntityEventInMemoryDbProvider _provider;
        private static TestEventSink? _currentSink;

        /// <summary>
        /// Создаёт набор тестов и сбрасывает состояние событийного слоя.
        /// </summary>
        public EntityEventListenerContractTests()
        {
            EntityEventInMemoryDbProvider.ResetState();
            _provider = new EntityEventInMemoryDbProvider("in-memory", new PostgresEngine());
            global::Titanic.Entity.EntityManager.ResetServices();
            EmployeeEventListener.IsEnabled = false;
            DepartmentCancelEventListener.IsEnabled = false;
        }

        /// <summary>
        /// Проверяет, что атрибут обработчика хранит имя Entity-таблицы.
        /// </summary>
        [Fact]
        public void EntityEventListenerAttribute_ShouldStoreEntityName()
        {
            var attribute = new EntityEventListenerAttribute("employees");

            Assert.Equal("employees", attribute.EntityName);
        }

        /// <summary>
        /// Проверяет, что отмена события помечает pipeline остановленным.
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
        /// Проверяет, что базовые обработчики событий ничего не делают по умолчанию.
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
        /// Проверяет, что сохранение новой сущности вызывает pipeline вставки.
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
        /// Проверяет, что новая запись с заданным первичным ключом проходит pipeline вставки.
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
        /// Проверяет, что сохранение существующей сущности вызывает pipeline обновления.
        /// </summary>
        [Fact]
        public void Entity_Save_ShouldCallUpdateEventPipeline()
        {
            var sink = new TestEventSink();
            ConfigureEntityServices(sink);
            var manager = CreateManager();
            var entity = CreateEmployee(manager, salary: 100m);
            entity.Save();
            sink.Clear();

            entity.Set(nameof(OrmEmployeeEntity.Salary), 150m);
            entity.Save();

            Assert.Equal(
                new[] { "saving:employees", "updating:employees", "updated:employees", "saved:employees" },
                sink.Events);
            Assert.Equal(100m, sink.LastOldSalary);
            Assert.Equal(150m, entity.OldValues[nameof(OrmEmployeeEntity.Salary)]);
        }

        /// <summary>
        /// Проверяет, что fallback update->insert использует pipeline фактической вставки.
        /// </summary>
        [Fact]
        public void Entity_Save_WhenExistingRecordIsMissing_ShouldUseInsertPipeline()
        {
            var sink = new TestEventSink();
            ConfigureEntityServices(sink);
            var manager = CreateManager(new MissingUpdateDbProvider());
            var manualPrimaryKey = Random.Shared.Next(2_000_001, 3_000_000);
            var entity = manager.Create("employees", CreateUserConnection(), isNew: false)
                .Set(nameof(OrmEmployeeEntity.Id), manualPrimaryKey)
                .Set(nameof(OrmEmployeeEntity.Name), $"EVT-FALLBACK-{Guid.NewGuid():N}")
                .Set(nameof(OrmEmployeeEntity.Email), $"{Guid.NewGuid():N}@mail.test")
                .Set(nameof(OrmEmployeeEntity.Salary), 300m)
                .Set(nameof(OrmEmployeeEntity.IsActive), true);

            Assert.False(entity.IsNew);

            entity.Save();

            Assert.Equal(
                new[] { "saving:employees", "inserting:employees", "inserted:employees", "saved:employees" },
                sink.Events);
            Assert.False(entity.IsNew);
        }

        /// <summary>
        /// Проверяет, что удаление существующей сущности вызывает pipeline удаления.
        /// </summary>
        [Fact]
        public void Entity_Delete_ShouldCallDeleteEventPipeline()
        {
            var sink = new TestEventSink();
            ConfigureEntityServices(sink);
            var manager = CreateManager();
            var entity = CreateEmployee(manager);
            entity.Save();
            sink.Clear();

            var deleted = entity.Delete();

            Assert.True(deleted);
            Assert.Equal(
                new[] { "deleting:employees", "deleted:employees" },
                sink.Events);
        }

        /// <summary>
        /// Проверяет, что listener может остановить сохранение сущности.
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
        /// Создаёт тестовую ORM-сущность сотрудника.
        /// </summary>
        private global::Titanic.Entity.Orm.Entity CreateEntity()
        {
            return global::Titanic.Entity.EntityManager.Create(
                typeof(OrmEmployeeEntity),
                _provider,
                CreateUserConnection());
        }

        /// <summary>
        /// Создаёт и инициализирует тестовый Entity ORM менеджер.
        /// </summary>
        private BaseEntityManager CreateManager()
        {
            return CreateManager(_provider);
        }

        /// <summary>
        /// Создаёт и инициализирует тестовый Entity ORM менеджер с заданным provider-ом.
        /// </summary>
        /// <param name="provider">Provider БД для тестового менеджера.</param>
        /// <returns>Тестовый Entity ORM менеджер.</returns>
        private static BaseEntityManager CreateManager(BaseDbProvider provider)
        {
            var manager = new EntityDbManager();
            manager.Initialize(
                "default",
                provider,
                new EntityManagerSettings
                {
                    EntityModelNamespaces = new List<string> { typeof(OrmEmployeeEntity).Namespace! }
                });
            return manager;
        }

        /// <summary>
        /// Создаёт новую тестовую сущность сотрудника.
        /// </summary>
        /// <param name="manager">Entity ORM менеджер.</param>
        /// <param name="salary">Зарплата сотрудника.</param>
        /// <returns>Новая ORM-сущность сотрудника.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateEmployee(BaseEntityManager manager, decimal salary = 100m)
        {
            return manager.Create<OrmEmployeeEntity>(CreateUserConnection())
                .Set(nameof(OrmEmployeeEntity.Name), $"EVT-{Guid.NewGuid():N}")
                .Set(nameof(OrmEmployeeEntity.Email), $"{Guid.NewGuid():N}@mail.test")
                .Set(nameof(OrmEmployeeEntity.Salary), salary)
                .Set(nameof(OrmEmployeeEntity.IsActive), true);
        }

        /// <summary>
        /// Создаёт аргументы события для заданной стадии pipeline.
        /// </summary>
        private static EntityEventArgs CreateArgs(EntityEventStage stage)
        {
            return new EntityEventArgs(
                new EntityDbManager(),
                stage);
        }

        /// <summary>
        /// Создаёт тестовый пользовательский контекст.
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
        /// Настраивает DI и включает тестовые listener-ы.
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

        /// <summary>
        /// Тестовый listener без переопределённых обработчиков.
        /// </summary>
        private sealed class TestEntityEventListener : BaseEntityEventListener
        {
        }

        /// <summary>
        /// Тестовый listener событий сотрудников.
        /// </summary>
        [EntityEventListener("employees")]
        private sealed class EmployeeEventListener : BaseEntityEventListener
        {
            /// <summary>
            /// Признак включения listener-а в текущем тесте.
            /// </summary>
            public static bool IsEnabled { get; set; }

            private readonly string _entityName;

            /// <summary>
            /// Создаёт listener для указанной Entity-таблицы.
            /// </summary>
            /// <param name="entityName">Имя Entity-таблицы.</param>
            public EmployeeEventListener(string entityName)
            {
                _entityName = entityName;
            }

            /// <summary>
            /// Фиксирует начало сохранения сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"saving:{_entityName}");
            }

            /// <summary>
            /// Фиксирует начало вставки сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnInserting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"inserting:{_entityName}");
            }

            /// <summary>
            /// Фиксирует начало обновления сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnUpdating(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"updating:{_entityName}");
                if (entity.OldValues.TryGetValue(nameof(OrmEmployeeEntity.Salary), out var salary))
                {
                    _currentSink!.LastOldSalary = salary;
                }
            }

            /// <summary>
            /// Фиксирует завершение обновления сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnUpdated(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"updated:{_entityName}");
            }

            /// <summary>
            /// Фиксирует завершение вставки сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnInserted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"inserted:{_entityName}");
            }

            /// <summary>
            /// Фиксирует завершение сохранения сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnSaved(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"saved:{_entityName}");
            }

            /// <summary>
            /// Фиксирует начало удаления сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnDeleting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"deleting:{_entityName}");
            }

            /// <summary>
            /// Фиксирует завершение удаления сотрудника.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
            public override void OnDeleted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                _currentSink?.Events.Add($"deleted:{_entityName}");
            }
        }

        /// <summary>
        /// Тестовый listener, отменяющий сохранение подразделения.
        /// </summary>
        [EntityEventListener("departments")]
        private sealed class DepartmentCancelEventListener : BaseEntityEventListener
        {
            /// <summary>
            /// Признак включения listener-а в текущем тесте.
            /// </summary>
            public static bool IsEnabled { get; set; }

            private readonly string _entityName;

            /// <summary>
            /// Создаёт listener для указанной Entity-таблицы.
            /// </summary>
            /// <param name="entityName">Имя Entity-таблицы.</param>
            public DepartmentCancelEventListener(string entityName)
            {
                _entityName = entityName;
            }

            /// <summary>
            /// Отменяет сохранение подразделения на стадии OnSaving.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <param name="args">Аргументы события.</param>
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

        /// <summary>
        /// Накопитель вызванных стадий событийного pipeline.
        /// </summary>
        private sealed class TestEventSink
        {
            /// <summary>
            /// Список зафиксированных событий.
            /// </summary>
            public List<string> Events { get; } = new();

            /// <summary>
            /// Старое значение зарплаты, полученное update-listener-ом.
            /// </summary>
            public object? LastOldSalary { get; set; }

            /// <summary>
            /// Очищает накопленные события и снимки между операциями одного теста.
            /// </summary>
            public void Clear()
            {
                Events.Clear();
                LastOldSalary = null;
            }
        }

        /// <summary>
        /// Provider, который моделирует отсутствие строки при UPDATE.
        /// </summary>
        private sealed class MissingUpdateDbProvider : BaseDbProvider
        {
            /// <summary>
            /// Создаёт тестовый provider.
            /// </summary>
            public MissingUpdateDbProvider()
                : base("in-memory", new PostgresEngine())
            {
            }

            /// <inheritdoc />
            public override int Execute(IQuery query)
            {
                return query is Update ? 0 : 1;
            }

            /// <inheritdoc />
            public override T ExecuteScalar<T>(IQuery query)
            {
                if (query is Select)
                {
                    return default!;
                }

                object result = typeof(T) switch
                {
                    var type when type == typeof(Guid) => Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    var type when type == typeof(int) => 1,
                    var type when type == typeof(long) => 1L,
                    var type when type == typeof(string) => "in-memory",
                    var type when type == typeof(object) => 1,
                    _ => Activator.CreateInstance<T>()!
                };

                return (T)result;
            }

            /// <inheritdoc />
            public override List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
            {
                return [];
            }

            /// <inheritdoc />
            protected override DbConnection CreateConnection()
            {
                throw new NotSupportedException("MissingUpdateDbProvider does not create database connections.");
            }

            /// <inheritdoc />
            protected override DbParameter CreateParameter(QueryParameter parameter)
            {
                throw new NotSupportedException("MissingUpdateDbProvider does not create database parameters.");
            }
        }

        #endregion Members
    }
}
