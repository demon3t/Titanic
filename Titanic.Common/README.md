# Titanic.Common

## Роль в архитектуре

`Titanic.Common` — нижний инфраструктурный слой решения. Он задаёт общий пользовательский контекст и web-механику, которыми пользуются `Titanic.Db` и `Titanic.Entity`.

Пакет не знает о SQL builder и Entity ORM, поэтому может использоваться как самостоятельная база для авторизации, `UserConnection` и ASP.NET Core-инфраструктуры.

## Когда использовать

Используйте `Titanic.Common`, если нужно:

- описать пользователя и его культуру через `UserConnection`;
- организовать авторизацию по заголовку для Entity API;
- подключить Swagger, API explorer и базовую web-инфраструктуру;
- получить общий слой, который не зависит от `Titanic.Db` и `Titanic.Entity`.

## Основные типы

### Пользовательский контекст

- `UserConnection` — контекст пользователя, содержащий `UserId` и `UserCulture`.
- `UserCulture` — культура пользователя, содержащая `Id` и `Name`.

### Авторизация

- `IAuthorizationCollection` — контракт коллекции авторизаций по ключу.
- `EntityAuthorizationCollection` — in-memory хранилище ключей авторизации для Entity API.
- `EntityAuthorizationRequirement` — authorization requirement для Entity API.
- `EntityAuthorizationHandler` — обработчик авторизации по заголовку.
- `MockEntityAuthorizationHandler` — тестовый обработчик для локальной отладки.
- `BaseHeaderAuthorizationHandler<TCollection, TRequirement>` — базовый handler для чтения ключа из заголовка.
- `BaseMockHeaderAuthorizationHandler<TCollection, TRequirement>` — базовый mock handler.

### Фабрика классов

- `ClassFactory` — статическая фабрика регистрации и создания объектов по типу или имени.
- `ConstructorArgument` — именованный аргумент конструктора для ручной передачи значений при создании объекта.

### ASP.NET Core extensions

- `WebApplicationExtensions.AddHeaderAuthorization<THandler, TCollection, TRequirement>(...)`
- `WebApplicationExtensions.AddEmptyAuthentication()`
- `WebApplicationExtensions.AddTitanicInfrastructure()`

## Пример использования

```csharp
using Titanic.Common.Services.Authorization.Entity;
using Titanic.Common.Session;
using Titanic.Common.WebApplication;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddTitanicInfrastructure()
    .AddEmptyAuthentication()
    .AddHeaderAuthorization<
        MockEntityAuthorizationHandler,
        EntityAuthorizationCollection,
        EntityAuthorizationRequirement>("Entity");
```

Создание `UserConnection`:

```csharp
var userConnection = new UserConnection
{
    UserId = Guid.NewGuid(),
    Culture = new UserCulture
    {
        Id = Guid.NewGuid(),
        Name = "ru-RU"
    }
};
```

## Связь с другими слоями

- `Titanic.Db` использует `Titanic.Common` как базовый инфраструктурный слой.
- `Titanic.Entity` использует `Titanic.Common` для `UserConnection`, культуры и авторизации API.

Если вы разбираете архитектуру целиком, дальше переходите к [Titanic.Db/README.md](../Titanic.Db/README.md).

## Связанные документы

- [../ARCHITECTURE.md](../ARCHITECTURE.md)
- [../Titanic.Db/README.md](../Titanic.Db/README.md)
- [../Titanic.Entity/README.md](../Titanic.Entity/README.md)
