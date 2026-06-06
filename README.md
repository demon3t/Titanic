# Titanic

`Titanic` — набор базовых .NET-библиотек для построения backend-решений поверх собственного SQL builder, Entity ORM и HTTP API-контракта для UI.

Проект разделён на три основных слоя:

- `Titanic.Common` — пользовательский контекст и web-инфраструктура;
- `Titanic.Db` — построение и выполнение SQL-запросов;
- `Titanic.Entity` — metadata-модель сущностей, ORM и HTTP API.

## Что находится в репозитории

### Базовые пакеты

- `Titanic.Common` — общие типы, авторизация по заголовкам, ASP.NET Core extensions.
- `Titanic.Db` — SQL expression model, fluent builders, провайдеры и SQL engine.
- `Titanic.Entity` — Entity ORM, структура metadata, Entity API, batch API.

### Вспомогательные и прикладные проекты

- `Titanic.Test` — unit- и integration-тесты для базовых пакетов.
- `Titanic.EntityApi` — отладочное приложение для проверки Entity API.
- `titanic-entity-react` — клиентская библиотека для работы с Entity API из React.
- `titanic-entity-react-demo` — демонстрационное приложение для React-библиотеки.
- `ui-example` — локальный UI-пример и площадка для проверки идей интерфейса.

## Как читать архитектуру

Если нужно быстро понять решение, начинайте в таком порядке:

1. [ARCHITECTURE.md](ARCHITECTURE.md) — карта слоёв, зависимости и поток данных.
2. [Titanic.Common/README.md](Titanic.Common/README.md) — базовый пользовательский контекст и web-инфраструктура.
3. [Titanic.Db/README.md](Titanic.Db/README.md) — SQL builder и низкоуровневый доступ к данным.
4. [Titanic.Entity/README.md](Titanic.Entity/README.md) — Entity ORM, metadata и HTTP API.
5. [Titanic.Entity/ENTITY_API.md](Titanic.Entity/ENTITY_API.md) — контракт API для frontend и SDK.

## Архитектурная идея

`Titanic` не строится вокруг Entity Framework Core. Вместо этого решение использует собственную цепочку слоёв:

- `Titanic.Common` формирует `UserConnection`, культуру и базовую web-инфраструктуру.
- `Titanic.Db` строит SQL и выполняет запросы к конкретной БД.
- `Titanic.Entity` описывает сущности, колонки, связи и поверх этого публикует ORM и HTTP API.
- UI или внешние сервисы работают с Entity API или напрямую с Entity/Db-слоем в зависимости от сценария.

Это позволяет:

- использовать только SQL builder там, где нужен прямой контроль над запросами;
- поднимать Entity ORM и API там, где нужен декларативный контракт для клиентских приложений;
- отделять инфраструктурный слой от прикладных UI и debug-проектов.

## Где искать нужную часть решения

- Нужен пользовательский контекст, авторизация или общая ASP.NET Core инфраструктура — [Titanic.Common/README.md](Titanic.Common/README.md).
- Нужен fluent SQL builder, `SELECT/INSERT/UPDATE/DELETE`, DDL и провайдеры БД — [Titanic.Db/README.md](Titanic.Db/README.md).
- Нужны Entity-модели, ORM-пути, локализация колонок и автоматическое API — [Titanic.Entity/README.md](Titanic.Entity/README.md).
- Нужен JSON-контракт для frontend или SDK — [Titanic.Entity/ENTITY_API.md](Titanic.Entity/ENTITY_API.md).

## Типовые сценарии использования

### Использовать только SQL builder

Когда нужен полный контроль над SQL, но хочется избежать ручной сборки строк, используется `Titanic.Db`.

### Использовать Entity ORM внутри backend

Когда модель данных удобнее описывать через сущности, связи и metadata, используется `Titanic.Entity` поверх `Titanic.Db`.

### Поднять API для UI

Когда UI должен работать через единый backend-контракт, используется `Titanic.Entity` с автоматической публикацией HTTP endpoint-ов.

## Пример слоя Titanic.Db

```csharp
var rows = provider.Select()
    .Column("e", "id")
    .Column("e", "name")
    .From("employees").As("e")
    .Where("e", "is_active").IsEqual(Column.Parameter(true))
    .OrderBy("e", "name")
    .ExecuteReader(r => new
    {
        Id = r.Get<int>("id"),
        Name = r.Get<string>("name")
    });
```

## Пример слоя Titanic.Entity

```csharp
var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("Id")
    .AddColumn("Name")
    .AddColumn("DepartmentId.Name", "DepartmentName")
    .AddFilter(EntityComparisonType.Contains, "Email", "@company.com")
    .OrderBy("Name")
    .GetEntityCollection();
```

## Связанные документы

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [Titanic.Common/README.md](Titanic.Common/README.md)
- [Titanic.Db/README.md](Titanic.Db/README.md)
- [Titanic.Entity/README.md](Titanic.Entity/README.md)
- [Titanic.Entity/ENTITY_API.md](Titanic.Entity/ENTITY_API.md)
