using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Titanic.Common.Session;
using Titanic.Db.Configuration;
using Titanic.Db.PosgreSql;
using Titanic.Db.WebApplication;
using Titanic.Entity.Attributes;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication;
using Titanic.Entity.WebApplication.Configuration;
using Xunit.Abstractions;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Сравнительные тесты производительности событийных слоёв local/http/gRPC при одинаковой listener-логике.
    /// </summary>
    public sealed class EntityEventPerformanceTests
    {
        #region Members

        private const string LocalManagerName = "EventLayerSpeedLocal";
        private const string HttpManagerName = "EventLayerSpeedHttp";
        private const string GrpcManagerName = "EventLayerSpeedGrpc";
        private const string WebSocketManagerName = "EventLayerSpeedWebSocket";
        private const string HttpListenerPath = "/entity-event-listener/event-layer-speed-http";
        private const string WebSocketListenerPath = "/entity-event-listener/event-layer-speed-ws";
        private const string FilledDescription = "filled-by-speed-listener";

        private readonly ITestOutputHelper _output;

        /// <summary>
        /// Создаёт набор тестов сравнительной скорости событийных transport-слоёв.
        /// </summary>
        /// <param name="output">Тестовый вывод xUnit.</param>
        public EntityEventPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Возвращает набор сценариев для сравнения производительности разных событийных pipeline.
        /// </summary>
        public static IEnumerable<object[]> GetPerformanceScenarios()
        {
            yield return
            [
                new PerformanceScenario(
                    "insert-small",
                    Iterations: 60,
                    WarmupIterations: 5,
                    MeasureRounds: 3,
                    CreateEntity: CreateNewEntity,
                    Execute: static entity => entity.Save(),
                    ExpectedCounters: new SpeedEventCounters(
                        Saving: 60,
                        Saved: 60,
                        Inserting: 60,
                        Inserted: 60,
                        Updating: 0,
                        Updated: 0,
                        Deleting: 0,
                        Deleted: 0,
                        DescriptionMutations: 60))
            ];

            yield return
            [
                new PerformanceScenario(
                    "insert-large",
                    Iterations: 250,
                    WarmupIterations: 10,
                    MeasureRounds: 3,
                    CreateEntity: CreateNewEntity,
                    Execute: static entity => entity.Save(),
                    ExpectedCounters: new SpeedEventCounters(
                        Saving: 250,
                        Saved: 250,
                        Inserting: 250,
                        Inserted: 250,
                        Updating: 0,
                        Updated: 0,
                        Deleting: 0,
                        Deleted: 0,
                        DescriptionMutations: 250))
            ];

            yield return
            [
                new PerformanceScenario(
                    "update-existing",
                    Iterations: 150,
                    WarmupIterations: 8,
                    MeasureRounds: 3,
                    CreateEntity: CreateExistingEntityForUpdate,
                    Execute: static entity => entity.Save(),
                    ExpectedCounters: new SpeedEventCounters(
                        Saving: 150,
                        Saved: 150,
                        Inserting: 0,
                        Inserted: 0,
                        Updating: 150,
                        Updated: 150,
                        Deleting: 0,
                        Deleted: 0,
                        DescriptionMutations: 150))
            ];

            yield return
            [
                new PerformanceScenario(
                    "delete-existing",
                    Iterations: 150,
                    WarmupIterations: 8,
                    MeasureRounds: 3,
                    CreateEntity: CreateExistingEntityForDelete,
                    Execute: static entity => entity.Delete(),
                    ExpectedCounters: new SpeedEventCounters(
                        Saving: 0,
                        Saved: 0,
                        Inserting: 0,
                        Inserted: 0,
                        Updating: 0,
                        Updated: 0,
                        Deleting: 150,
                        Deleted: 150,
                        DescriptionMutations: 0))
            ];
        }

        /// <summary>
        /// Сравнивает local/http/gRPC для одного и того же сценария событийного pipeline.
        /// </summary>
        /// <param name="scenario">Сценарий измерения.</param>
        [Theory]
        [MemberData(nameof(GetPerformanceScenarios))]
        public async Task Entity_EventLayers_ShouldMeasureSpeed_WithSameListenerLogic(PerformanceScenario scenario)
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();

            var localManager = CreateInMemoryManager(LocalManagerName, null);
            var httpManager = CreateInMemoryManager(HttpManagerName, listenerApp.GetHttpListenerUri(HttpListenerPath));
            var grpcManager = CreateInMemoryManager(GrpcManagerName, listenerApp.GrpcListenerUri);
            var webSocketManager = CreateInMemoryManager(WebSocketManagerName, listenerApp.GetWebSocketListenerUri(WebSocketListenerPath));

            var local = MeasureLayer("local", localManager, scenario);
            var http = MeasureLayer("http", httpManager, scenario);
            var grpc = MeasureLayer("grpc", grpcManager, scenario);
            var webSocket = MeasureLayer("websocket", webSocketManager, scenario);

            WriteResults(scenario, local, http, grpc, webSocket);

            Assert.Equal(scenario.Iterations, local.CompletedOperations);
            Assert.Equal(scenario.Iterations, http.CompletedOperations);
            Assert.Equal(scenario.Iterations, grpc.CompletedOperations);
            Assert.Equal(scenario.Iterations, webSocket.CompletedOperations);

            Assert.True(
                local.MedianElapsed <= http.MedianElapsed,
                $"Local listener должен быть не медленнее HTTP в сценарии '{scenario.Name}': local={local.MedianElapsed.TotalMilliseconds:F2}ms, http={http.MedianElapsed.TotalMilliseconds:F2}ms.");
            Assert.True(
                local.MedianElapsed <= grpc.MedianElapsed,
                $"Local listener должен быть не медленнее gRPC в сценарии '{scenario.Name}': local={local.MedianElapsed.TotalMilliseconds:F2}ms, grpc={grpc.MedianElapsed.TotalMilliseconds:F2}ms.");
            Assert.True(
                local.MedianElapsed <= webSocket.MedianElapsed,
                $"Local listener должен быть не медленнее WebSocket в сценарии '{scenario.Name}': local={local.MedianElapsed.TotalMilliseconds:F2}ms, websocket={webSocket.MedianElapsed.TotalMilliseconds:F2}ms.");
        }

        /// <summary>
        /// Выполняет warmup и несколько раундов измерения для одного событийного слоя.
        /// </summary>
        /// <param name="layerName">Имя измеряемого слоя.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="scenario">Сценарий измерения.</param>
        /// <returns>Результат измерения слоя.</returns>
        private static EventLayerSpeedResult MeasureLayer(
            string layerName,
            BaseEntityManager manager,
            PerformanceScenario scenario)
        {
            RunBatch(manager, scenario, scenario.WarmupIterations, validateCounters: false);

            var rounds = new List<TimeSpan>(scenario.MeasureRounds);
            for (var i = 0; i < scenario.MeasureRounds; i++)
            {
                rounds.Add(RunBatch(manager, scenario, scenario.Iterations, validateCounters: true));
            }

            var ordered = rounds.OrderBy(x => x).ToArray();
            return new EventLayerSpeedResult(
                layerName,
                scenario.Iterations,
                rounds,
                ordered[ordered.Length / 2]);
        }

        /// <summary>
        /// Выполняет пакет операций одного сценария и возвращает общее время выполнения.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="scenario">Сценарий измерения.</param>
        /// <param name="iterations">Количество операций.</param>
        /// <param name="validateCounters">Проверять ли счётчики listener-а после выполнения.</param>
        /// <returns>Общее время выполнения пакета.</returns>
        private static TimeSpan RunBatch(
            BaseEntityManager manager,
            PerformanceScenario scenario,
            int iterations,
            bool validateCounters)
        {
            SpeedEventSink.Reset();
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var entity = scenario.CreateEntity(manager, i);
                var completed = scenario.Execute(entity);

                Assert.True(completed, $"Сценарий '{scenario.Name}' должен завершаться успешно.");
                if (!string.Equals(scenario.Name, "delete-existing", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Equal(FilledDescription, entity.Get<string>(nameof(OrmEventLayerSpeedEntity.Description)));
                }
            }

            stopwatch.Stop();

            if (validateCounters)
            {
                AssertCounters(scenario.ExpectedCounters);
            }

            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Проверяет, что listener вызвал ожидаемые стадии pipeline нужное число раз.
        /// </summary>
        /// <param name="expected">Ожидаемые значения счётчиков.</param>
        private static void AssertCounters(SpeedEventCounters expected)
        {
            Assert.Equal(expected.Saving, SpeedEventSink.SavingCount);
            Assert.Equal(expected.Saved, SpeedEventSink.SavedCount);
            Assert.Equal(expected.Inserting, SpeedEventSink.InsertingCount);
            Assert.Equal(expected.Inserted, SpeedEventSink.InsertedCount);
            Assert.Equal(expected.Updating, SpeedEventSink.UpdatingCount);
            Assert.Equal(expected.Updated, SpeedEventSink.UpdatedCount);
            Assert.Equal(expected.Deleting, SpeedEventSink.DeletingCount);
            Assert.Equal(expected.Deleted, SpeedEventSink.DeletedCount);
            Assert.Equal(expected.DescriptionMutations, SpeedEventSink.DescriptionMutationCount);
        }

        /// <summary>
        /// Выводит результаты по сценариям и транспортам в xUnit output.
        /// </summary>
        /// <param name="scenario">Сценарий измерения.</param>
        /// <param name="results">Результаты замеров.</param>
        private void WriteResults(PerformanceScenario scenario, params EventLayerSpeedResult[] results)
        {
            _output.WriteLine($"Сценарий '{scenario.Name}', операций: {scenario.Iterations}, раундов: {scenario.MeasureRounds}");
            foreach (var result in results.OrderBy(x => x.MedianElapsed))
            {
                _output.WriteLine(
                    $"{result.LayerName}: median={result.MedianElapsed.TotalMilliseconds:F2} ms, avg/op={result.AveragePerOperation.TotalMilliseconds:F4} ms, rounds=[{string.Join(", ", result.RoundElapsed.Select(x => $"{x.TotalMilliseconds:F2}"))}]");
            }
        }

        /// <summary>
        /// Создаёт новую сущность для сценариев вставки.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="index">Порядковый номер операции.</param>
        /// <returns>Новая ORM-сущность.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateNewEntity(BaseEntityManager manager, int index)
        {
            return manager.Create<OrmEventLayerSpeedEntity>(CreateUserConnection())
                .Set(nameof(OrmEventLayerSpeedEntity.Name), $"speed-insert-{index}-{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Создаёт существующую сущность для сценария обновления.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="index">Порядковый номер операции.</param>
        /// <returns>Существующая ORM-сущность.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateExistingEntityForUpdate(BaseEntityManager manager, int index)
        {
            var entity = CreateExistingEntity(manager, index, $"old-description-{index}");
            entity.Set(nameof(OrmEventLayerSpeedEntity.Description), $"new-description-{index}");
            return entity;
        }

        /// <summary>
        /// Создаёт существующую сущность для сценария удаления.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="index">Порядковый номер операции.</param>
        /// <returns>Существующая ORM-сущность.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateExistingEntityForDelete(BaseEntityManager manager, int index)
        {
            return CreateExistingEntity(manager, index, $"delete-description-{index}");
        }

        /// <summary>
        /// Создаёт существующую ORM-сущность на основе select-builder без обращения к реальной БД.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="index">Порядковый номер операции.</param>
        /// <param name="description">Исходное описание сущности.</param>
        /// <returns>Существующая ORM-сущность.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateExistingEntity(
            BaseEntityManager manager,
            int index,
            string description)
        {
            var builder = manager.Select<OrmEventLayerSpeedEntity>(CreateUserConnection());
            builder.AddColumn(nameof(OrmEventLayerSpeedEntity.Id));
            builder.AddColumn(nameof(OrmEventLayerSpeedEntity.Name));
            builder.AddColumn(nameof(OrmEventLayerSpeedEntity.Description));

            return builder.CreateRecord(new Dictionary<string, object?>
            {
                [nameof(OrmEventLayerSpeedEntity.Id)] = index + 1,
                [nameof(OrmEventLayerSpeedEntity.Name)] = $"speed-existing-{index}-{Guid.NewGuid():N}",
                [nameof(OrmEventLayerSpeedEntity.Description)] = description
            });
        }

        /// <summary>
        /// Запускает listener API с in-memory provider-ом для HTTP и gRPC сценариев.
        /// </summary>
        /// <returns>Запущенное тестовое listener-приложение.</returns>
        private static Task<EntityEventListenerTestApplication> CreateInMemoryListenerAppAsync()
        {
            ResetRuntimeState();
            return EntityEventListenerTestApplication.StartAsync(builder =>
            {
                builder.Logging.ClearProviders();

                builder.AddTitanicDb(config =>
                {
                    config.DefaultProviderName = "EventLayerSpeedInMemory";
                    config.Providers =
                    [
                        new DbProviderConfig
                        {
                            Name = "EventLayerSpeedInMemory",
                            ConnectionString = "in-memory",
                            Types = new ProviderTypeConfig
                            {
                                ProviderType = typeof(EntityEventInMemoryDbProvider).AssemblyQualifiedName!,
                                EngineType = typeof(PostgresEngine).AssemblyQualifiedName!
                            }
                        }
                    ];
                });

                builder.AddTitanicEntityEventListenerApi(config =>
                {
                    config.Managers =
                    [
                        new EntityManagerSettings
                        {
                            Name = HttpManagerName,
                            DbProviderName = "EventLayerSpeedInMemory",
                            EntityModelNamespaces = [typeof(OrmEventLayerSpeedEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.Http,
                                Path = HttpListenerPath
                            }
                        },
                        new EntityManagerSettings
                        {
                            Name = GrpcManagerName,
                            DbProviderName = "EventLayerSpeedInMemory",
                            EntityModelNamespaces = [typeof(OrmEventLayerSpeedEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.Grpc
                            }
                        },
                        new EntityManagerSettings
                        {
                            Name = WebSocketManagerName,
                            DbProviderName = "EventLayerSpeedInMemory",
                            EntityModelNamespaces = [typeof(OrmEventLayerSpeedEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.WebSocket,
                                Path = WebSocketListenerPath
                            }
                        }
                    ];
                });
            });
        }

        /// <summary>
        /// Создаёт менеджер Entity ORM поверх in-memory provider-а.
        /// </summary>
        /// <param name="managerName">Имя менеджера.</param>
        /// <param name="eventListener">URI удалённого listener-а или <see langword="null" /> для local режима.</param>
        /// <returns>Инициализированный менеджер.</returns>
        private static BaseEntityManager CreateInMemoryManager(string managerName, string? eventListener)
        {
            var provider = new EntityEventInMemoryDbProvider("in-memory", new PostgresEngine());
            var manager = new EntityDbManager();
            manager.Initialize(
                managerName,
                provider,
                new EntityManagerSettings
                {
                    EntityModelNamespaces = [typeof(OrmEventLayerSpeedEntity).Namespace!],
                    EventListener = eventListener
                });

            return manager;
        }

        /// <summary>
        /// Создаёт тестовый пользовательский контекст.
        /// </summary>
        /// <returns>Пользовательский контекст.</returns>
        private static UserConnection CreateUserConnection()
        {
            return new UserConnection
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Culture = new UserCulture
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Test"
                }
            };
        }

        /// <summary>
        /// Сбрасывает глобальное состояние перед тестом производительности.
        /// </summary>
        private static void ResetRuntimeState()
        {
            SpeedEventListener.IsEnabled = true;
            SpeedEventSink.Reset();
            global::Titanic.Entity.EntityManager.Reset();
            global::Titanic.Entity.EntityManager.ResetServices();
        }

        [EntityEventListener("event_layer_speed_entities")]
        private sealed class SpeedEventListener : BaseEntityEventListener
        {
            public static bool IsEnabled { get; set; }

            /// <summary>
            /// Создаёт listener для тестовой сущности speed-замеров.
            /// </summary>
            /// <param name="entityName">Имя Entity-таблицы.</param>
            public SpeedEventListener(string entityName)
            {
            }

            /// <summary>
            /// Выполняет одинаковую бизнес-логику для всех transport-слоёв на стадии сохранения.
            /// </summary>
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                entity.Set(nameof(OrmEventLayerSpeedEntity.Description), FilledDescription);
                SpeedEventSink.IncrementSaving();
                SpeedEventSink.IncrementDescriptionMutation();
            }

            /// <summary>
            /// Фиксирует стадию Saved.
            /// </summary>
            public override void OnSaved(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementSaved();
                }
            }

            /// <summary>
            /// Фиксирует стадию Inserting.
            /// </summary>
            public override void OnInserting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementInserting();
                }
            }

            /// <summary>
            /// Фиксирует стадию Inserted.
            /// </summary>
            public override void OnInserted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementInserted();
                }
            }

            /// <summary>
            /// Фиксирует стадию Updating.
            /// </summary>
            public override void OnUpdating(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementUpdating();
                }
            }

            /// <summary>
            /// Фиксирует стадию Updated.
            /// </summary>
            public override void OnUpdated(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementUpdated();
                }
            }

            /// <summary>
            /// Фиксирует стадию Deleting.
            /// </summary>
            public override void OnDeleting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementDeleting();
                }
            }

            /// <summary>
            /// Фиксирует стадию Deleted.
            /// </summary>
            public override void OnDeleted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    SpeedEventSink.IncrementDeleted();
                }
            }
        }

        /// <summary>
        /// Описание сценария замера производительности.
        /// </summary>
        public sealed record PerformanceScenario(
            string Name,
            int Iterations,
            int WarmupIterations,
            int MeasureRounds,
            Func<BaseEntityManager, int, global::Titanic.Entity.Orm.Entity> CreateEntity,
            Func<global::Titanic.Entity.Orm.Entity, bool> Execute,
            SpeedEventCounters ExpectedCounters);

        /// <summary>
        /// Снимок ожидаемых счётчиков pipeline для одного пакета операций.
        /// </summary>
        public sealed record SpeedEventCounters(
            int Saving,
            int Saved,
            int Inserting,
            int Inserted,
            int Updating,
            int Updated,
            int Deleting,
            int Deleted,
            int DescriptionMutations);

        /// <summary>
        /// Итог измерения одного transport-слоя.
        /// </summary>
        private sealed record EventLayerSpeedResult(
            string LayerName,
            int CompletedOperations,
            IReadOnlyList<TimeSpan> RoundElapsed,
            TimeSpan MedianElapsed)
        {
            public TimeSpan AveragePerOperation => TimeSpan.FromTicks(MedianElapsed.Ticks / CompletedOperations);
        }

        private static class SpeedEventSink
        {
            private static readonly object SyncRoot = new();
            private static int _savingCount;
            private static int _savedCount;
            private static int _insertingCount;
            private static int _insertedCount;
            private static int _updatingCount;
            private static int _updatedCount;
            private static int _deletingCount;
            private static int _deletedCount;
            private static int _descriptionMutationCount;

            public static int SavingCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _savingCount;
                    }
                }
            }

            public static int SavedCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _savedCount;
                    }
                }
            }

            public static int InsertingCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _insertingCount;
                    }
                }
            }

            public static int InsertedCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _insertedCount;
                    }
                }
            }

            public static int UpdatingCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _updatingCount;
                    }
                }
            }

            public static int UpdatedCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _updatedCount;
                    }
                }
            }

            public static int DeletingCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _deletingCount;
                    }
                }
            }

            public static int DeletedCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _deletedCount;
                    }
                }
            }

            public static int DescriptionMutationCount
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _descriptionMutationCount;
                    }
                }
            }

            /// <summary>
            /// Сбрасывает накопленные метрики listener-а.
            /// </summary>
            public static void Reset()
            {
                lock (SyncRoot)
                {
                    _savingCount = 0;
                    _savedCount = 0;
                    _insertingCount = 0;
                    _insertedCount = 0;
                    _updatingCount = 0;
                    _updatedCount = 0;
                    _deletingCount = 0;
                    _deletedCount = 0;
                    _descriptionMutationCount = 0;
                }
            }

            public static void IncrementSaving()
            {
                lock (SyncRoot)
                {
                    _savingCount++;
                }
            }

            public static void IncrementSaved()
            {
                lock (SyncRoot)
                {
                    _savedCount++;
                }
            }

            public static void IncrementInserting()
            {
                lock (SyncRoot)
                {
                    _insertingCount++;
                }
            }

            public static void IncrementInserted()
            {
                lock (SyncRoot)
                {
                    _insertedCount++;
                }
            }

            public static void IncrementUpdating()
            {
                lock (SyncRoot)
                {
                    _updatingCount++;
                }
            }

            public static void IncrementUpdated()
            {
                lock (SyncRoot)
                {
                    _updatedCount++;
                }
            }

            public static void IncrementDeleting()
            {
                lock (SyncRoot)
                {
                    _deletingCount++;
                }
            }

            public static void IncrementDeleted()
            {
                lock (SyncRoot)
                {
                    _deletedCount++;
                }
            }

            public static void IncrementDescriptionMutation()
            {
                lock (SyncRoot)
                {
                    _descriptionMutationCount++;
                }
            }
        }

        #endregion Members
    }
}
