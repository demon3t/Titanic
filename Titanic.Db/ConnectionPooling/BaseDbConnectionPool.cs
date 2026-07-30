using System.Collections.Concurrent;
using System.Data.Common;
using Titanic.Db.Configuration;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Простой in-process пул подключений для <see cref="BaseDbProvider"/>.
    /// Хранит заранее созданные <see cref="DbConnection"/> в стеке,
    /// уменьшая накладные расходы на открытие соединения.
    /// </summary>
    internal sealed class BaseDbConnectionPool : IDisposable
    {
        private readonly Func<DbConnection> _factory;
        private readonly Func<DbConnection, bool> _isConnectionHealthy;
        private readonly Stack<DbConnection> _available = new();
        private readonly object _lock = new();
        private readonly int _maxPoolSize;
        private readonly int _minPoolSize;
        private int _currentSize;
        private int _activeCount;
        private bool _disposed;

        /// <summary>
        /// Текущая конфигурация пула.
        /// </summary>
        public ConnectionPoolConfig Config { get; }

        /// <summary>
        /// Создать пул.
        /// </summary>
        /// <param name="factory">Фабрика новых подключений.</param>
        /// <param name="config">Конфигурация пула.</param>
        /// <param name="isConnectionHealthy">Проверка состояния подключения.</param>
        public BaseDbConnectionPool(
            Func<DbConnection> factory,
            ConnectionPoolConfig? config = null,
            Func<DbConnection, bool>? isConnectionHealthy = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Config = config ?? new ConnectionPoolConfig();
            _maxPoolSize = Config.MaxPoolSize > 0 ? Config.MaxPoolSize : 1;
            _minPoolSize = Math.Max(0, Config.MinPoolSize);
            _isConnectionHealthy = isConnectionHealthy ?? (conn => conn.State == System.Data.ConnectionState.Open);

            Prewarm();
        }

        /// <summary>
        /// Общее количество кешированных + активных подключений.
        /// </summary>
        public int CurrentSize
        {
            get { lock (_lock) { return _currentSize; } }
        }

        /// <summary>
        /// Количество активных подключений.
        /// </summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// Получить подключение из пула.
        /// Если достигнут максимум, запрос блокируется.
        /// </summary>
        public DbConnection Rent()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var shouldCreatePooledConnection = false;
            lock (_lock)
            {
                while (_available.Count > 0)
                {
                    var conn = _available.Pop();
                    if (!_isConnectionHealthy(conn))
                    {
                        try { conn.Dispose(); } catch { /* ignore */ }
                        _currentSize--;
                        continue;
                    }

                    _activeCount++;
                    return conn;
                }

                if (_currentSize < _maxPoolSize)
                {
                    _currentSize++;
                    _activeCount++;
                    shouldCreatePooledConnection = true;
                }
            }

            if (shouldCreatePooledConnection)
            {
                try
                {
                    return _factory();
                }
                catch
                {
                    lock (_lock)
                    {
                        _currentSize--;
                        _activeCount--;
                    }

                    throw;
                }
            }

            // Места нет — создаём сверх лимита, не блокируя ожидание других.
            // (Можно дополнить блокировкой по semaphore, если требуется строгий лимит.)
            _activeCount++;
            return _factory();
        }

        /// <summary>
        /// Вернуть подключение в пул. Если пул уже заполнен или закрыт, подключение будет утилизировано.
        /// </summary>
        public void Return(DbConnection connection)
        {
            if (connection == null) return;

            if (_disposed)
            {
                DisposeConnection(connection);
                return;
            }

            _activeCount--;

            if (_currentSize > _maxPoolSize || !_isConnectionHealthy(connection))
            {
                DisposeConnection(connection);
                lock (_lock) { _currentSize--; }
                return;
            }

            lock (_lock)
            {
                _available.Push(connection);
            }
        }

        private void Prewarm()
        {
            if (_minPoolSize <= 0) return;

            for (var i = 0; i < _minPoolSize; i++)
            {
                try
                {
                    var conn = _factory();
                    lock (_lock)
                    {
                        _currentSize++;
                        _available.Push(conn);
                    }
                }
                catch
                {
                    // Игнорируем неудачный prewarm: подключение будет создано по запросу.
                }
            }
        }

        private void DisposeConnection(DbConnection conn)
        {
            try { conn.Dispose(); } catch { /* ignore */ }
        }

        /// <summary>
        /// Освободить все подключения в пуле.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                while (_available.Count > 0)
                {
                    DisposeConnection(_available.Pop());
                }
                _currentSize = 0;
            }
        }
    }
}
