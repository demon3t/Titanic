# Titanic

`Titanic` - набор базовых .NET-библиотек для построения backend-приложений поверх собственной SQL ORM и Entity ORM модели.

Репозиторий решает три основные задачи:

- типобезопасное построение SQL без ручной сборки строк;
- работа с Entity-моделями, путями колонок, фильтрами, локализацией и HTTP API;
- общий пользовательский контекст и инфраструктура авторизации для Entity API.

Проект не является Entity Framework Core. Здесь свой SQL builder, свой Entity ORM и свой HTTP-контракт для UI-клиентов.

## Что находится в решении

Текущее solution `Titanic.sln` содержит 4 основных проекта:

- `Titanic.Common`  
  Общая инфраструктура: `UserConnection`, `UserCulture`, header/mock авторизация, ASP.NET Core extension-методы.

- `Titanic.Db`  
  Низкоуровневый SQL builder и слой выполнения запросов.  
  Содержит `Select`, `Insert`, `Update`, `Delete`, `Table`, `Column`, `Func`, SQL engine/provider abstractions.

- `Titanic.Entity`  
  Entity ORM поверх `Titanic.Db`.  
  Содержит `Entity`, `EntityManager`, `EntitySchemaQuery`, JSON-модели запросов, автоматическую публикацию HTTP API, локализацию и metadata-модель сущностей.

- `Titanic.Test`  
  Unit и integration tests для `Titanic.Db`, `Titanic.Entity` и `Titanic.Common`.

## Что ещё есть в репозитории

Кроме solution, в рабочем каталоге есть вспомогательные проекты и локальные инструменты:

- `Titanic.EntityApi`  
  Отладочное/демо ASP.NET Core приложение для проверки Entity API.

- `titanic-entity-react`  
  React/TypeScript клиентская библиотека для работы с Entity API.

- `titanic-entity-react-demo`  
  Демонстрационное приложение для клиентской библиотеки.

- `ui-example`  
  Локальная ссылка на внешний UI-пример.

- `tools\ps`  
  Локальные PowerShell-скрипты для быстрых проверок репозитория перед commit/push.

Часть этих каталогов используется как локальная среда разработки и не входит в `Titanic.sln`.

## Архитектура

Слои идут снизу вверх:

1. `Titanic.Common`  
   Общие типы и web-инфраструктура.

2. `Titanic.Db`  
   SQL AST, builders, providers, engines, выполнение команд.

3. `Titanic.Entity`  
   Entity metadata, ESQ-запросы, materialization, `Save/Delete`, HTTP API.

4. Клиенты и демо  
   `Titanic.EntityApi`, `titanic-entity-react`, `titanic-entity-react-demo`.

## Для чего это нужно

Если коротко, стек нужен для такого сценария:

- описать таблицы через CLR-модели и атрибуты;
- строить запросы по ORM-путям вроде `DepartmentId.Name` или `[EmployeeId:Id:Id].City`;
- получать сущности и их `DisplayValue`;
- сохранять и удалять сущности;
- автоматически поднимать HTTP API для UI;
- использовать один и тот же контракт на backend и frontend.

## Быстрый старт

### 1. Собрать решение

```powershell
dotnet build C:\Titanic\Titanic.sln --nologo
```

### 2. Прогнать тесты

```powershell
dotnet test C:\Titanic\Titanic.Test\Titanic.Test.csproj --nologo
```

Если локальная тестовая БД настроена через `Titanic.Test\appsettings.Test.local.json`, тесты можно запускать прямо из IDE или одной командой без ручной передачи строки подключения.

### 3. Быстрые локальные проверки

```powershell
powershell -ExecutionPolicy Bypass -File C:\Titanic\tools\ps\Before-Commit.ps1
```

Перед push:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Titanic\tools\ps\Before-Push.ps1
```

## Пример использования слоя `Titanic.Db`

```csharp
var provider = DbManager.GetProvider("main");

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

## Пример использования слоя `Titanic.Entity`

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

## HTTP API

`Titanic.Entity` умеет автоматически публиковать API для UI.

Основной контракт описан в:

- [Titanic.Entity/ENTITY_API.md](C:/Titanic/Titanic.Entity/ENTITY_API.md)

Базовые endpoint-ы:

- `POST {ApiPath}` - одна операция (`Select`, `Save`, `Delete`)
- `POST {ApiPath}/batch` - пакет операций

## Где читать дальше

- [Titanic.Common/README.md](C:/Titanic/Titanic.Common/README.md)
- [Titanic.Db/README.md](C:/Titanic/Titanic.Db/README.md)
- [Titanic.Entity/README.md](C:/Titanic/Titanic.Entity/README.md)
- [Titanic.Entity/ENTITY_API.md](C:/Titanic/Titanic.Entity/ENTITY_API.md)

