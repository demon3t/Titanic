using System.Reflection;

namespace Titanic.Common.Services.Factory
{
    /// <summary>
    /// Статическая фабрика регистрации и создания объектов по типу или имени.
    /// </summary>
    public static class ClassFactory
    {
        #region Members

        private static readonly object BindingsLock = new();
        private static readonly List<ClassFactoryBinding> Bindings = new();
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Инициализирует новый экземпляр Configure.
        /// </summary>
        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Инициализирует новый экземпляр Reset.
        /// </summary>
        public static void Reset()
        {
            _serviceProvider = null;
            lock (BindingsLock)
            {
                Bindings.Clear();
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр Bind.
        /// </summary>
        public static void Bind(Type service, Type implementation)
        {
            Bind(service, implementation, string.Empty);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Bind.
        /// </summary>
        public static void Bind(Type service, Type implementation, string name)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(implementation);

            if (!service.IsAssignableFrom(implementation))
            {
                throw new InvalidOperationException(
                    $"Type '{implementation.FullName}' is not assignable to '{service.FullName}'.");
            }

            AddBinding(new ClassFactoryBinding(service, NormalizeName(name), implementation, null));
        }

        /// <summary>
        /// Регистрирует биндинг сервиса.
        /// </summary>
        public static void Bind<T>(Type classType) where T : class
        {
            Bind(typeof(T), classType);
        }

        /// <summary>
        /// Регистрирует биндинг сервиса.
        /// </summary>
        public static void Bind<T>(Func<T> resolveMethod) where T : class
        {
            Bind(resolveMethod, string.Empty);
        }

        /// <summary>
        /// Регистрирует биндинг сервиса.
        /// </summary>
        public static void Bind<T>(Func<T> resolveMethod, string name) where T : class
        {
            ArgumentNullException.ThrowIfNull(resolveMethod);
            AddBinding(new ClassFactoryBinding(
                typeof(T),
                NormalizeName(name),
                null,
                () => resolveMethod()
                    ?? throw new InvalidOperationException($"Factory method for '{typeof(T).FullName}' returned null.")));
        }

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        public static void Bind<T, TImpl>()
            where T : class
            where TImpl : T
        {
            Bind(typeof(T), typeof(TImpl));
        }

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        public static void Bind<T, TImpl>(string name)
            where T : class
            where TImpl : T
        {
            Bind(typeof(T), typeof(TImpl), name);
        }

        /// <summary>
        /// Создаёт объект по полному имени типа без обязательного биндинга.
        /// </summary>
        public static T ForceGet<T>(string fullClassName, params ConstructorArgument[] constructorArguments)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(fullClassName))
            {
                throw new ArgumentException("Class name is empty.", nameof(fullClassName));
            }

            var implementationType = ResolveType(fullClassName)
                ?? throw new InvalidOperationException($"Type '{fullClassName}' not found.");

            return CreateTypedInstance<T>(implementationType, constructorArguments);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Get.
        /// </summary>
        public static object Get(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (TryFindFirstBinding(type, string.Empty, out var binding))
            {
                return CreateInstance(binding, Array.Empty<ConstructorArgument>());
            }

            return CreateByImplementation(type, Array.Empty<ConstructorArgument>());
        }

        /// <summary>
        /// Получает экземпляр сервиса.
        /// </summary>
        public static T Get<T>(params ConstructorArgument[] constructorArguments) where T : class
        {
            return Get<T>(string.Empty, constructorArguments);
        }

        /// <summary>
        /// Получает экземпляр сервиса.
        /// </summary>
        public static T Get<T>(string name, params ConstructorArgument[] constructorArguments) where T : class
        {
            if (TryFindFirstBinding(typeof(T), name, out var binding))
            {
                return (T)CreateInstance(binding, constructorArguments);
            }

            return CreateTypedInstance<T>(typeof(T), constructorArguments);
        }

        /// <summary>
        /// Получает все экземпляры сервиса.
        /// </summary>
        public static IEnumerable<T> GetAll<T>(params ConstructorArgument[] constructorArguments) where T : class
        {
            return GetAll<T>(string.Empty, constructorArguments);
        }

        /// <summary>
        /// Получает все экземпляры сервиса.
        /// </summary>
        public static IEnumerable<T> GetAll<T>(string name, params ConstructorArgument[] constructorArguments) where T : class
        {
            var bindings = GetBindings(typeof(T), name);
            if (bindings.Count == 0)
            {
                return typeof(T).IsInterface || typeof(T).IsAbstract
                    ? Enumerable.Empty<T>()
                    : new[] { CreateTypedInstance<T>(typeof(T), constructorArguments) };
            }

            return bindings
                .Select(binding => (T)CreateInstance(binding, constructorArguments))
                .ToArray();
        }

        /// <summary>
        /// Инициализирует новый экземпляр HasBinding.
        /// </summary>
        public static bool HasBinding(Type type)
        {
            return HasBinding(type, null);
        }

        /// <summary>
        /// Инициализирует новый экземпляр HasBinding.
        /// </summary>
        public static bool HasBinding(Type type, string? name)
        {
            ArgumentNullException.ThrowIfNull(type);
            var normalizedName = NormalizeOptionalName(name);

            lock (BindingsLock)
            {
                return Bindings.Any(binding =>
                    binding.ServiceType == type &&
                    (normalizedName == null || binding.Name == normalizedName));
            }
        }

        /// <summary>
        /// Проверяет наличие биндинга.
        /// </summary>
        public static bool HasBinding<T>() where T : class
        {
            return HasBinding(typeof(T));
        }

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        public static void ReBind<T, TImpl>()
            where T : class
            where TImpl : T
        {
            ReBind<T, TImpl>(string.Empty);
        }

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        public static void ReBind<T, TImpl>(string name)
            where T : class
            where TImpl : T
        {
            RemoveBindings(typeof(T), name);
            Bind<T, TImpl>(name);
        }

        /// <summary>
        /// Перерегистрирует биндинг на фабричный метод.
        /// </summary>
        public static void RebindWithFactoryMethod<T>(Func<T> resolveMethod) where T : class
        {
            RebindWithFactoryMethod(resolveMethod, string.Empty);
        }

        /// <summary>
        /// Перерегистрирует биндинг на фабричный метод.
        /// </summary>
        public static void RebindWithFactoryMethod<T>(Func<T> resolveMethod, string name) where T : class
        {
            ArgumentNullException.ThrowIfNull(resolveMethod);
            RemoveBindings(typeof(T), name);
            Bind(resolveMethod, name);
        }

        /// <summary>
        /// Пытается получить именованный экземпляр сервиса.
        /// </summary>
        public static bool TryGet<T>(string name, out T? instance, params ConstructorArgument[] constructorArguments)
            where T : class
        {
            if (!TryFindFirstBinding(typeof(T), name, out var binding))
            {
                instance = null;
                return false;
            }

            instance = (T)CreateInstance(binding, constructorArguments);
            return true;
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddBinding.
        /// </summary>
        private static void AddBinding(ClassFactoryBinding binding)
        {
            lock (BindingsLock)
            {
                Bindings.Add(binding);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр RemoveBindings.
        /// </summary>
        private static void RemoveBindings(Type serviceType, string name)
        {
            var normalizedName = NormalizeName(name);

            lock (BindingsLock)
            {
                Bindings.RemoveAll(binding =>
                    binding.ServiceType == serviceType &&
                    binding.Name == normalizedName);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр TryFindFirstBinding.
        /// </summary>
        private static bool TryFindFirstBinding(Type serviceType, string name, out ClassFactoryBinding binding)
        {
            var normalizedName = NormalizeName(name);
            ClassFactoryBinding? found;

            lock (BindingsLock)
            {
                found = Bindings.FirstOrDefault(current =>
                    current.ServiceType == serviceType &&
                    current.Name == normalizedName);
            }

            binding = found!;
            return found != null;
        }

        /// <summary>
        /// Инициализирует новый экземпляр GetBindings.
        /// </summary>
        private static List<ClassFactoryBinding> GetBindings(Type serviceType, string name)
        {
            var normalizedName = NormalizeName(name);

            lock (BindingsLock)
            {
                return Bindings
                    .Where(binding => binding.ServiceType == serviceType && binding.Name == normalizedName)
                    .ToList();
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateInstance.
        /// </summary>
        private static object CreateInstance(ClassFactoryBinding binding, IReadOnlyCollection<ConstructorArgument> constructorArguments)
        {
            if (binding.FactoryMethod != null)
            {
                return binding.FactoryMethod();
            }

            return CreateByImplementation(binding.ImplementationType!, constructorArguments);
        }

        /// <summary>
        /// Создаёт типизированный экземпляр.
        /// </summary>
        private static T CreateTypedInstance<T>(Type implementationType, IReadOnlyCollection<ConstructorArgument> constructorArguments)
            where T : class
        {
            var instance = CreateByImplementation(implementationType, constructorArguments);
            if (instance is T typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"Type '{implementationType.FullName}' is not assignable to '{typeof(T).FullName}'.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateByImplementation.
        /// </summary>
        private static object CreateByImplementation(Type implementationType, IReadOnlyCollection<ConstructorArgument> constructorArguments)
        {
            ArgumentNullException.ThrowIfNull(implementationType);

            if (implementationType.IsAbstract || implementationType.IsInterface)
            {
                throw new InvalidOperationException(
                    $"Type '{implementationType.FullName}' cannot be instantiated directly.");
            }

            // Нормализуем аргументы по имени: последнее значение побеждает.
            var normalizedArguments = constructorArguments
                .GroupBy(argument => argument.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            // Сначала пробуем конструктор с максимальным количеством параметров.
            foreach (var constructor in implementationType.GetConstructors()
                         .OrderByDescending(current => current.GetParameters().Length))
            {
                if (TryBuildConstructorArguments(constructor, normalizedArguments, out var resolvedArguments))
                {
                    return constructor.Invoke(resolvedArguments);
                }
            }

            var argumentNames = normalizedArguments.Count == 0
                ? "none"
                : string.Join(", ", normalizedArguments.Keys);

            throw new InvalidOperationException(
                    $"No matching constructor found for '{implementationType.FullName}'. Constructor arguments: {argumentNames}.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр TryBuildConstructorArguments.
        /// </summary>
        private static bool TryBuildConstructorArguments(
            ConstructorInfo constructor,
            IReadOnlyDictionary<string, ConstructorArgument> constructorArguments,
            out object?[] resolvedArguments)
        {
            var parameters = constructor.GetParameters();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            resolvedArguments = new object?[parameters.Length];
            var serviceProvider = GetServiceProvider();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                // Сначала используем явно переданный аргумент по имени.
                if (constructorArguments.TryGetValue(parameter.Name!, out var argument))
                {
                    if (!TryConvertValue(argument.Value, parameter.ParameterType, out var convertedValue))
                    {
                        return false;
                    }

                    usedNames.Add(argument.Name);
                    resolvedArguments[i] = convertedValue;
                    continue;
                }

                // Затем подставляем IServiceProvider или сервис из контейнера.
                if (parameter.ParameterType == typeof(IServiceProvider))
                {
                    resolvedArguments[i] = serviceProvider;
                    continue;
                }

                var service = serviceProvider.GetService(parameter.ParameterType);
                if (service != null)
                {
                    resolvedArguments[i] = service;
                    continue;
                }

                // Для необязательного параметра используем значение по умолчанию.
                if (parameter.HasDefaultValue)
                {
                    resolvedArguments[i] = parameter.DefaultValue;
                    continue;
                }

                return false;
            }

            return constructorArguments.Keys.All(usedNames.Contains);
        }

        /// <summary>
        /// Инициализирует новый экземпляр GetServiceProvider.
        /// </summary>
        private static IServiceProvider GetServiceProvider()
        {
            return _serviceProvider
                ?? throw new InvalidOperationException("ClassFactory service provider is not configured.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр TryConvertValue.
        /// </summary>
        private static bool TryConvertValue(object? value, Type targetType, out object? convertedValue)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value == null)
            {
                convertedValue = null;
                return !underlyingType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            }

            if (underlyingType.IsInstanceOfType(value))
            {
                convertedValue = value;
                return true;
            }

            if (underlyingType.IsEnum)
            {
                if (value is string stringValue && Enum.TryParse(underlyingType, stringValue, true, out var enumValue))
                {
                    convertedValue = enumValue;
                    return true;
                }

                if (TryChangeType(value, Enum.GetUnderlyingType(underlyingType), out var numericValue))
                {
                    convertedValue = Enum.ToObject(underlyingType, numericValue!);
                    return true;
                }
            }

            // Guid обрабатываем отдельно, не полагаясь только на общее преобразование.
            if (underlyingType == typeof(Guid))
            {
                if (value is Guid guidValue)
                {
                    convertedValue = guidValue;
                    return true;
                }

                if (value is string guidString && Guid.TryParse(guidString, out var parsedGuid))
                {
                    convertedValue = parsedGuid;
                    return true;
                }
            }

            if (TryChangeType(value, underlyingType, out var changedValue))
            {
                convertedValue = changedValue;
                return true;
            }

            convertedValue = null;
            return false;
        }

        /// <summary>
        /// Инициализирует новый экземпляр TryChangeType.
        /// </summary>
        private static bool TryChangeType(object value, Type targetType, out object? convertedValue)
        {
            try
            {
                convertedValue = Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                convertedValue = null;
                return false;
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResolveType.
        /// </summary>
        private static Type? ResolveType(string fullClassName)
        {
            var type = Type.GetType(fullClassName, false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullClassName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeName.
        /// </summary>
        private static string NormalizeName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? string.Empty : name;
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeOptionalName.
        /// </summary>
        private static string? NormalizeOptionalName(string? name)
        {
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private sealed record ClassFactoryBinding(
            Type ServiceType,
            string Name,
            Type? ImplementationType,
            Func<object>? FactoryMethod);

        #endregion Members
    }
}
