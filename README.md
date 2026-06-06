# Titanic

`Titanic` — платформа для построения backend-решений на .NET поверх собственного SQL builder и Entity ORM.

Репозиторий объединяет три базовых слоя:

- `Titanic.Common` — общий пользовательский контекст и web-инфраструктура;
- `Titanic.Db` — низкоуровневое построение и выполнение SQL-запросов;
- `Titanic.Entity` — Entity ORM, metadata-модель сущностей и HTTP API для клиентских приложений.

Проект не использует Entity Framework Core как основной механизм доступа к данным. Вместо этого он предоставляет собственную модель построения SQL, Entity-запросов и API-контрактов для UI.

## Назначение

`Titanic` решает следующие задачи:

- типобезопасное построение SQL без ручной сборки строк;
- работа с Entity-моделями, колонками, связями, фильтрами и локализацией;
- сохранение и удаление сущностей через единый ORM-слой;
- автоматическая публикация HTTP API для работы с Entity ORM;
- единый контракт взаимодействия между backend и frontend.

## Состав решения

Файл `Titanic.sln` включает следующие проекты:

### Titanic.Common

Базовый инфраструктурный пакет.

Содержит:

- `UserConnection` и `UserCulture`;
- header/mock авторизацию;
- общие ASP.NET Core extension-методы;
- вспомогательные типы для работы `Titanic.Entity` API.

### Titanic.Db

Низкоуровневый слой доступа к данным.

Содержит:

- SQL AST и expression model;
- fluent builders для `SELECT`, `INSERT`, `UPDATE`, `DELETE`;
- DDL helper `Table`;
- `Column`, `Func`, `QueryExpression`;
- абстракции провайдера и SQL-движка;
- реализацию PostgreSQL provider/engine.

### Titanic.Entity

Entity ORM поверх `Titanic.Db`.

Содержит:

- `Entity`, `ColumnValue`, `ReferenceColumnValue`;
- `EntityManager` и `BaseEntityManager`;
- `EntitySchemaQuery` и query model;
- metadata-структуру сущностей и колонок;
- локализацию по metadata;
- HTTP API и batch API для UI-клиентов.

### Titanic.Test

Набор unit- и integration-тестов для основных библиотек.

## Архитектурная модель

Слои выстроены последовательно:

1. `Titanic.Common` — общие типы и инфраструктура.
2. `Titanic.Db` — SQL builder и исполнение запросов.
3. `Titanic.Entity` — Entity ORM и HTTP API.
4. Клиентские и демонстрационные приложения используют эти слои как базу.

Такое разделение позволяет:

- использовать только `Titanic.Db`, если нужен прямой контроль над SQL;
- использовать `Titanic.Entity`, если нужна Entity-модель и API поверх неё;
- не смешивать прикладную бизнес-логику с механизмом построения SQL.

## Ключевые возможности

### SQL builder

`Titanic.Db` предоставляет fluent API для построения SQL:

- параметризованные условия;
- `JOIN`, `GROUP BY`, `HAVING`, `ORDER BY`, paging;
- `CASE`, агрегатные функции, подзапросы;
- DDL-операции над таблицами.

Пример:

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

### Entity ORM

`Titanic.Entity` строит запросы по ORM-путям и metadata-модели сущностей.

Поддерживаются:

- выборка колонок по путям вида `DepartmentId.Name`;
- выборка по обратным связям;
- фильтрация, сортировка, агрегации и paging;
- `Save()` и `Delete()` для сущностей;
- `DisplayValue` для ссылочных колонок;
- локализуемые колонки.

Пример:

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

### HTTP API

`Titanic.Entity` умеет автоматически публиковать endpoint-ы для UI.

Базовая модель API:

- `POST {ApiPath}` — одна операция;
- `POST {ApiPath}/batch` — пакет операций.

Поддерживаемые операции:

- `Select`
- `Save`
- `Delete`

Подробный контракт описан в [Titanic.Entity/ENTITY_API.md](C:/Titanic/Titanic.Entity/ENTITY_API.md).

## Дополнительные каталоги репозитория

Помимо solution, в репозитории есть вспомогательные и демонстрационные каталоги:

- `Titanic.EntityApi` — отладочное web-приложение для проверки Entity API;
- `titanic-entity-react` — React/TypeScript клиентская библиотека;
- `titanic-entity-react-demo` — демонстрационное приложение для клиентской библиотеки;
- `ui-example` — локальный UI-пример.

Они используются как прикладной или демонстрационный слой поверх основных библиотек.

## Для кого этот репозиторий

Репозиторий полезен, если требуется:

- построить backend без EF Core, но с типизированным SQL builder;
- описывать БД через Entity metadata-модели;
- быстро поднимать HTTP API для клиентских приложений;
- использовать единый backend/frontend контракт для запросов к данным.

## Документация по проектам

Подробные описания находятся в отдельных README:

- [Titanic.Common/README.md](C:/Titanic/Titanic.Common/README.md)
- [Titanic.Db/README.md](C:/Titanic/Titanic.Db/README.md)
- [Titanic.Entity/README.md](C:/Titanic/Titanic.Entity/README.md)
- [Titanic.Entity/ENTITY_API.md](C:/Titanic/Titanic.Entity/ENTITY_API.md)
