using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// ORM-сущность, представляющая одну запись БД и хранящая значения колонок в словаре.
    /// </summary>
    public sealed class Entity
    {
        #region Fields

        /// <summary>
        /// Значения считанных или установленных колонок по SQL-алиасу или имени колонки.
        /// </summary>
        private readonly Dictionary<string, ColumnValue> _values;

        /// <summary>
        /// Соответствие ORM-пути к SQL-алиасу выбранной колонки.
        /// </summary>
        private readonly Dictionary<string, string> _pathToAlias;

        /// <summary>
        /// Метаданные колонок по SQL-алиасу или ORM-пути.
        /// </summary>
        private readonly Dictionary<string, ColumnStructure> _aliasToColumn;

        /// <summary>
        /// Метаданные корневой сущности.
        /// </summary>
        private readonly EntityStructure _structure;

        /// <summary>
        /// Провайдер БД, через который выполняются Save/Delete операции.
        /// </summary>
        private readonly BaseDbProvider _provider;

        /// <summary>
        /// Контекст пользователя, обязательный для работы Entity ORM.
        /// </summary>
        private readonly UserConnection _userConnection;

        /// <summary>
        /// Менеджер Entity ORM, создавший текущую сущность.
        /// </summary>
        private readonly BaseEntityManager? _manager;

        /// <summary>
        /// Признак того, что сущность была создана как новая запись и ещё не была сохранена.
        /// </summary>
        private bool _isNew;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Создать ORM-сущность из набора значений и метаданных выбранных колонок.
        /// </summary>
        /// <param name="values"> Значения колонок по алиасам. </param>
        /// <param name="pathToAlias"> Соответствие ORM-путей к алиасам. </param>
        /// <param name="aliasToColumn"> Метаданные колонок по алиасам. </param>
        /// <param name="structure"> Метаданные корневой сущности. </param>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        internal Entity(
            Dictionary<string, ColumnValue> values,
            Dictionary<string, string> pathToAlias,
            Dictionary<string, ColumnStructure> aliasToColumn,
            EntityStructure structure,
            BaseDbProvider provider,
            UserConnection userConnection,
            bool isNew,
            BaseEntityManager? manager = null)
        {
            _values = values;
            _pathToAlias = pathToAlias;
            _aliasToColumn = aliasToColumn;
            _structure = structure;
            _provider = provider;
            _userConnection = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
            _isNew = isNew;
            _manager = manager;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Контекст пользователя, с которым была создана или считана сущность.
        /// </summary>
        public UserConnection UserConnection => _userConnection;

        /// <summary>
        /// Значения колонок по SQL-алиасам выбранных колонок.
        /// </summary>
        public IReadOnlyDictionary<string, ColumnValue> Values => _values;

        /// <summary>
        /// Признак новой сущности, которая ещё не была сохранена в БД.
        /// </summary>
        public bool IsNew => _isNew;

        /// <summary>
        /// Имя таблицы корневой сущности.
        /// </summary>
        internal string TableName => _structure.TableName;

        /// <summary>
        /// Известные ORM-пути выбранных колонок и соответствующие им SQL-алиасы.
        /// </summary>
        public IReadOnlyDictionary<string, string> Paths => _pathToAlias;

        #endregion Properties

        #region Indexers

        /// <summary>
        /// Получить или установить сырое значение колонки по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Сырое значение колонки. </returns>
        public object? this[string pathOrAlias]
        {
            get => Get(pathOrAlias);
            set => Set(pathOrAlias, value);
        }

        #endregion Indexers

        #region Public Methods

        /// <summary>
        /// Проверить, есть ли в сущности значение по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> <see langword="true" />, если значение найдено. </returns>
        public bool Contains(string pathOrAlias)
        {
            var alias = ResolveAlias(pathOrAlias);
            return alias != null && _values.ContainsKey(alias);
        }

        /// <summary>
        /// Получить сырое значение колонки по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Сырое значение колонки или <see langword="null" />. </returns>
        public object? Get(string pathOrAlias)
        {
            return GetColumnValue(pathOrAlias)?.Value;
        }

        /// <summary>
        /// Получить отображаемое значение колонки по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Отображаемое значение колонки или <see langword="null" />. </returns>
        public object? GetDisplayValue(string pathOrAlias)
        {
            return GetColumnValue(pathOrAlias)?.DisplayValue;
        }

        /// <summary>
        /// Получить объект значения колонки по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Объект значения колонки или <see langword="null" />. </returns>
        public ColumnValue? GetColumnValue(string pathOrAlias)
        {
            var alias = ResolveAlias(pathOrAlias);
            return alias != null && _values.TryGetValue(alias, out var value) ? value : null;
        }

        /// <summary>
        /// Установить сырое значение колонки по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <param name="value"> Новое сырое значение. </param>
        /// <returns> Текущая сущность для fluent-цепочки. </returns>
        public Entity Set(string pathOrAlias, object? value)
        {
            if (string.IsNullOrWhiteSpace(pathOrAlias))
            {
                throw new ArgumentException("Entity value path is empty", nameof(pathOrAlias));
            }

            var alias = ResolveAlias(pathOrAlias) ?? pathOrAlias;
            var column = ResolveColumn(alias, pathOrAlias);
            var normalizedValue = NormalizeValue(column, value);
            if (_values.TryGetValue(alias, out var existing))
            {
                existing.Value = normalizedValue;
                return this;
            }

            _values[alias] = CreateColumnValue(alias, column, normalizedValue, displayValue: null);
            return this;
        }

        /// <summary>
        /// Установить несколько сырых значений колонок.
        /// </summary>
        /// <param name="values"> Значения по ORM-путям, именам колонок или SQL-алиасам. </param>
        /// <returns> Текущая сущность для fluent-цепочки. </returns>
        public Entity SetValues(IReadOnlyDictionary<string, object?> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            foreach (var value in values)
            {
                Set(value.Key, value.Value);
            }

            return this;
        }

        /// <summary>
        /// Создать новую запись или обновить существующую запись корневой сущности.
        /// </summary>
        /// <returns> <see langword="true" />, если операция выполнена. </returns>
        public bool Save()
        {
            DispatchEvent(EntityEventStage.Saving);

            if (!_isNew)
            {
                var primaryColumn = _structure.GetPrimaryColumnStructure();
                if (!TryGetColumnValue(primaryColumn, out var primaryValue) || IsDefaultPrimaryValue(primaryValue))
                {
                    throw new InvalidOperationException("Entity primary key value is required for Save() of existing record.");
                }

                DispatchEvent(EntityEventStage.Updating);

                if (UpdateEntity(primaryColumn, primaryValue) > 0)
                {
                    DispatchEvent(EntityEventStage.Updated);
                    DispatchEvent(EntityEventStage.Saved);
                    return true;
                }
            }

            var insertPrimaryColumn = _structure.GetPrimaryColumnStructure();
            var hasPrimaryValue = TryGetColumnValue(insertPrimaryColumn, out var insertPrimaryValue)
                && !IsDefaultPrimaryValue(insertPrimaryValue);
            DispatchEvent(EntityEventStage.Inserting);
            InsertEntity(includePrimary: hasPrimaryValue);
            _isNew = false;
            DispatchEvent(EntityEventStage.Inserted);
            DispatchEvent(EntityEventStage.Saved);
            return true;
        }

        /// <summary>
        /// Удалить запись корневой сущности по первичному ключу.
        /// </summary>
        /// <returns> <see langword="true" />, если строка была удалена. </returns>
        public bool Delete()
        {
            DispatchEvent(EntityEventStage.Deleting);

            var primaryColumn = _structure.GetPrimaryColumnStructure();
            if (!TryGetColumnValue(primaryColumn, out var primaryValue) || IsDefaultPrimaryValue(primaryValue))
            {
                throw new InvalidOperationException("Entity primary key value is required for Delete().");
            }

            var affected = _provider.Delete(_structure.TableName)
                .From(_structure.TableName, "t")
                .Where("t", primaryColumn.ColumnName)
                .IsEqual(Column.Parameter(primaryValue))
                .Execute();

            if (affected > 0)
            {
                DispatchEvent(EntityEventStage.Deleted);
            }

            return affected > 0;
        }

        /// <summary>
        /// Получить типизированное сырое значение колонки.
        /// </summary>
        /// <typeparam name="T"> Ожидаемый CLR-тип значения. </typeparam>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Типизированное значение или значение по умолчанию. </returns>
        public T? Get<T>(string pathOrAlias)
        {
            var value = Get(pathOrAlias);
            if (value == null || value is DBNull)
            {
                return default;
            }

            if (value is T typed)
            {
                return typed;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Получить типизированное отображаемое значение колонки.
        /// </summary>
        /// <typeparam name="T"> Ожидаемый CLR-тип отображаемого значения. </typeparam>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Типизированное отображаемое значение или значение по умолчанию. </returns>
        public T? GetDisplayValue<T>(string pathOrAlias)
        {
            var value = GetDisplayValue(pathOrAlias);
            if (value == null || value is DBNull)
            {
                return default;
            }

            if (value is T typed)
            {
                return typed;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Попытаться получить типизированное сырое значение колонки.
        /// </summary>
        /// <typeparam name="T"> Ожидаемый CLR-тип значения. </typeparam>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <param name="value"> Полученное значение или значение по умолчанию. </param>
        /// <returns> <see langword="true" />, если колонка найдена. </returns>
        public bool TryGet<T>(string pathOrAlias, out T? value)
        {
            if (!Contains(pathOrAlias))
            {
                value = default;
                return false;
            }

            value = Get<T>(pathOrAlias);
            return true;
        }

        /// <summary>
        /// Получить копию сырых значений колонок по SQL-алиасам.
        /// </summary>
        /// <returns> Словарь SQL-алиасов и сырых значений колонок. </returns>
        public Dictionary<string, object?> ToDictionary()
        {
            return _values.ToDictionary(
                x => x.Key,
                x => x.Value.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Создать объект значения колонки по метаданным колонки.
        /// </summary>
        /// <param name="alias"> SQL-алиас или имя колонки. </param>
        /// <param name="column"> Метаданные колонки. </param>
        /// <param name="value"> Сырое значение. </param>
        /// <param name="displayValue"> Отображаемое значение. </param>
        /// <returns> Объект значения колонки подходящего типа. </returns>
        internal static ColumnValue CreateColumnValue(
            string alias,
            ColumnStructure? column,
            object? value,
            object? displayValue)
        {
            if (column?.IsReference == true)
            {
                return new ReferenceColumnValue(alias, column.DataValueType, value, displayValue);
            }

            if (column?.DataValueType == DataValueType.String)
            {
                return new StringColumnValue(alias, value);
            }

            return new ScalarColumnValue(alias, column?.DataValueType ?? DataValueType.String, value);
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Найти SQL-алиас по ORM-пути, имени колонки или алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> SQL-алиас или <see langword="null" />. </returns>
        private string? ResolveAlias(string pathOrAlias)
        {
            if (_values.ContainsKey(pathOrAlias))
            {
                return pathOrAlias;
            }

            return _pathToAlias.TryGetValue(pathOrAlias, out var alias) ? alias : null;
        }

        /// <summary>
        /// Найти метаданные колонки по алиасу или исходному пути.
        /// </summary>
        /// <param name="alias"> SQL-алиас. </param>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <returns> Метаданные колонки или <see langword="null" />. </returns>
        private ColumnStructure? ResolveColumn(string alias, string pathOrAlias)
        {
            if (_aliasToColumn.TryGetValue(alias, out var aliasColumn))
            {
                return aliasColumn;
            }

            if (_aliasToColumn.TryGetValue(pathOrAlias, out var pathColumn))
            {
                return pathColumn;
            }

            return _structure.ColumnsStructure.FirstOrDefault(x => x.Matches(pathOrAlias) || x.Matches(alias));
        }

        /// <summary>
        /// Выполнить INSERT корневой сущности.
        /// </summary>
        /// <param name="includePrimary"> Включать ли первичный ключ в список вставляемых колонок. </param>
        private void InsertEntity(bool includePrimary)
        {
            var primaryColumn = _structure.GetPrimaryColumnStructure();
            var values = GetWritableColumns(includePrimary)
                .Select(column => new
                {
                    Column = column,
                    HasValue = TryGetColumnValue(column, out var value),
                    Value = value
                })
                .Where(x => x.HasValue)
                .ToList();

            if (values.Count == 0)
            {
                throw new InvalidOperationException("Entity has no values to insert.");
            }

            var insertedPrimaryValue = _provider.Insert(_structure.TableName)
                .SetColumns(values.Select(x => x.Column.ColumnName).ToArray())
                .Values(values.Select(x => Column.Parameter(x.Value)).ToArray())
                .Returning(primaryColumn.ColumnName)
                .ExecuteScalar<object>();

            SetColumnValue(primaryColumn, insertedPrimaryValue);
        }

        /// <summary>
        /// Выполнить UPDATE корневой сущности по первичному ключу.
        /// </summary>
        /// <param name="primaryColumn"> Метаданные первичной колонки. </param>
        /// <param name="primaryValue"> Значение первичного ключа. </param>
        /// <returns> Количество измененных строк. </returns>
        private int UpdateEntity(ColumnStructure primaryColumn, object? primaryValue)
        {
            var values = GetWritableColumns(includePrimary: false)
                .Select(column => new
                {
                    Column = column,
                    HasValue = TryGetColumnValue(column, out var value),
                    Value = value
                })
                .Where(x => x.HasValue)
                .ToList();

            if (values.Count == 0)
            {
                return 0;
            }

            var update = _provider.Update(_structure.TableName)
                .Table(_structure.TableName, "t");

            foreach (var value in values)
            {
                update.Set(value.Column.ColumnName, Column.Parameter(value.Value));
            }

            return update
                .Where("t", primaryColumn.ColumnName)
                .IsEqual(Column.Parameter(primaryValue))
                .Execute();
        }

        /// <summary>
        /// Получить колонки корневой сущности, доступные для записи.
        /// </summary>
        /// <param name="includePrimary"> Включать ли первичную колонку. </param>
        /// <returns> Список записываемых колонок. </returns>
        private IEnumerable<ColumnStructure> GetWritableColumns(bool includePrimary)
        {
            return _structure.ColumnsStructure.Where(column => includePrimary || !column.IsPrimary);
        }

        /// <summary>
        /// Попытаться получить сырое значение конкретной колонки.
        /// </summary>
        /// <param name="column"> Метаданные колонки. </param>
        /// <param name="value"> Найденное сырое значение. </param>
        /// <returns> <see langword="true" />, если значение найдено. </returns>
        private bool TryGetColumnValue(ColumnStructure column, out object? value)
        {
            if (TryGetValue(column.PropertyName, out value)
                || TryGetValue(column.ColumnName, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Попытаться получить сырое значение по ORM-пути, имени колонки или SQL-алиасу.
        /// </summary>
        /// <param name="pathOrAlias"> ORM-путь, имя колонки или SQL-алиас. </param>
        /// <param name="value"> Найденное сырое значение. </param>
        /// <returns> <see langword="true" />, если значение найдено. </returns>
        private bool TryGetValue(string pathOrAlias, out object? value)
        {
            var alias = ResolveAlias(pathOrAlias);
            if (alias != null && _values.TryGetValue(alias, out var columnValue))
            {
                value = columnValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Установить сырое значение конкретной колонки.
        /// </summary>
        /// <param name="column"> Метаданные колонки. </param>
        /// <param name="value"> Сырое значение. </param>
        private void SetColumnValue(ColumnStructure column, object? value)
        {
            Set(column.PropertyName, value);
        }

        /// <summary>
        /// Проверить, считается ли значение первичного ключа незаполненным.
        /// </summary>
        /// <param name="value"> Проверяемое значение первичного ключа. </param>
        /// <returns> <see langword="true" />, если значение не заполнено. </returns>
        private static bool IsDefaultPrimaryValue(object? value)
        {
            if (value == null || value is DBNull)
            {
                return true;
            }

            return value switch
            {
                int intValue => intValue == default,
                long longValue => longValue == default,
                Guid guidValue => guidValue == default,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                _ => false
            };
        }

        /// <summary>
        /// Нормализовать входное значение по типу колонки.
        /// </summary>
        /// <param name="column"> Метаданные колонки. </param>
        /// <param name="value"> Исходное значение. </param>
        /// <returns> Значение в CLR-типе, подходящем для провайдера БД. </returns>
        private static object? NormalizeValue(ColumnStructure? column, object? value)
        {
            if (column?.DataValueType == DataValueType.Guid
                && value is string stringValue
                && Guid.TryParse(stringValue, out var guidValue))
            {
                return guidValue;
            }

            return value;
        }

        /// <summary>
        /// Выполнить этап pipeline событийного слоя.
        /// </summary>
        /// <param name="stage"> Этап pipeline. </param>
        private void DispatchEvent(EntityEventStage stage)
        {
            if (_manager == null)
            {
                return;
            }

            EntityEventDispatcher.Dispatch(this, _manager, stage);
        }

        #endregion Private Methods
    }
}
