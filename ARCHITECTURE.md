# Архитектура Titanic

Документ помогает быстро понять структуру репозитория, роли проектов и путь прохождения данных через основные слои решения.

## Слои решения

Репозиторий разделён на три базовых пакета и набор прикладных проектов вокруг них.

### 1. Titanic.Common

Нижний инфраструктурный слой.

Отвечает за:

- `UserConnection` и `UserCulture`;
- базовую ASP.NET Core инфраструктуру;
- header/mock авторизацию для Entity API;
- общие типы, не зависящие от SQL builder и ORM.

Используйте этот слой, если нужен пользовательский контекст или web-инфраструктура без привязки к конкретной ORM-логике.

### 2. Titanic.Db

Слой построения и выполнения SQL.

Отвечает за:

- expression model и SQL AST;
- fluent builders для `SELECT`, `INSERT`, `UPDATE`, `DELETE`;
- SQL-функции, условия, `JOIN`, `GROUP BY`, `HAVING`, paging;
- DDL helper `Table`;
- провайдеры БД и SQL engine.

Этот слой можно использовать отдельно, если требуется прямой контроль над запросами.

### 3. Titanic.Entity

ORM-слой поверх `Titanic.Db`.

Отвечает за:

- Entity-модели и metadata таблиц/колонок;
- ORM-пути, связи и локализацию колонок;
- `EntityManager`, `EntitySchemaQuery`, `Entity`;
- сохранение и удаление сущностей;
- публикацию HTTP API для клиентских приложений.

Этот слой нужен, когда SQL builder уже недостаточен и требуется декларативная модель сущностей или API-контракт для UI.

## Зависимости между проектами

Базовые зависимости идут строго снизу вверх:

```text
Titanic.Common
      ↑
Titanic.Db
      ↑
Titanic.Entity
```

Фактическая зависимость в коде:

- `Titanic.Db` использует `Titanic.Common`.
- `Titanic.Entity` использует `Titanic.Common` и `Titanic.Db`.
- Вспомогательные проекты используют базовые пакеты, но не являются частью их архитектурного ядра.

## Прикладные и отладочные проекты

Эти проекты нужны для проверки и использования базовых пакетов, но не формируют базовую архитектуру:

- `Titanic.Test` — проверка базовых библиотек.
- `Titanic.EntityApi` — отладочное API-приложение поверх `Titanic.Entity`.
- `titanic-entity-react` — клиентская библиотека для frontend.
- `titanic-entity-react-demo` — демонстрация клиентской библиотеки.
- `ui-example` — локальный UI-пример.

При чтении решения важно сначала понимать `Common -> Db -> Entity`, и только потом переходить к demo/debug проектам.

## Поток данных

### Сценарий 1. Прямой SQL

```text
Application service
    -> Titanic.Db
    -> Db provider / engine
    -> Database
```

### Сценарий 2. Entity ORM внутри backend

```text
Application service
    -> Titanic.Entity
    -> Titanic.Db
    -> Database
```

### Сценарий 3. UI через HTTP API

```text
Frontend / SDK
    -> Titanic.Entity HTTP API
    -> Entity ORM
    -> Titanic.Db
    -> Database
```

### Сценарий 4. Авторизация и культура пользователя

```text
HTTP request
    -> Titanic.Common authorization provider
    -> UserConnection / UserCulture
    -> Titanic.Entity
    -> Titanic.Db
```

## Как выбрать точку входа

### Если вы backend-разработчик

Начинайте с:

1. [Titanic.Db/README.md](Titanic.Db/README.md), если нужен контроль над SQL.
2. [Titanic.Entity/README.md](Titanic.Entity/README.md), если нужен ORM-слой и metadata.

### Если вы frontend-разработчик

Начинайте с:

1. [Titanic.Entity/ENTITY_API.md](Titanic.Entity/ENTITY_API.md)
2. [Titanic.Entity/README.md](Titanic.Entity/README.md)
3. `titanic-entity-react` и `titanic-entity-react-demo`

### Если вы разбираете решение целиком

Порядок чтения:

1. [README.md](README.md)
2. [Titanic.Common/README.md](Titanic.Common/README.md)
3. [Titanic.Db/README.md](Titanic.Db/README.md)
4. [Titanic.Entity/README.md](Titanic.Entity/README.md)
5. [Titanic.Entity/ENTITY_API.md](Titanic.Entity/ENTITY_API.md)

## Что является базовым контрактом

Для внешних проектов базовыми контрактами являются:

- `UserConnection` и связанные типы из `Titanic.Common`;
- fluent SQL API и провайдеры из `Titanic.Db`;
- Entity-модели, `EntityManager`, `EntitySchemaQuery` и HTTP API из `Titanic.Entity`.

Demo- и debug-проекты не должны становиться источником архитектурных правил для базовых пакетов.

## Связанные документы

- [README.md](README.md)
- [Titanic.Common/README.md](Titanic.Common/README.md)
- [Titanic.Db/README.md](Titanic.Db/README.md)
- [Titanic.Entity/README.md](Titanic.Entity/README.md)
- [Titanic.Entity/ENTITY_API.md](Titanic.Entity/ENTITY_API.md)
