namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер для построения LIMIT/OFFSET.
    /// Возвращается методом <see cref="Select.Limit(int)"/>.
    /// <see cref="Limit(int)"/> и <see cref="Offset(int)"/> — fluent-цепочка (возвращают PagingItem).
    /// <see cref="Take(int)"/>, <see cref="Top(int)"/>, <see cref="Skip(int)"/>, <see cref="Page(int, int)"/> —
    /// терминальные методы, возвращающие <see cref="Select"/>.
    /// </summary>
    public class PagingItem
    {
        private readonly Select _select;

        internal PagingItem(Select select)
        {
            _select = select;
        }

        /// <summary>
        /// Установить LIMIT. Возвращает <see cref="PagingItem"/> для fluent-цепочки.
        /// </summary>
        public PagingItem Limit(int limit)
        {
            _select.LimitInternal(limit);
            return this;
        }

        /// <summary>
        /// Установить LIMIT и вернуть <see cref="Select"/>.
        /// </summary>
        public Select Take(int count)
        {
            _select.LimitInternal(count);
            return _select;
        }

        /// <summary>
        /// Установить LIMIT (Top) и вернуть <see cref="Select"/>.
        /// </summary>
        public Select Top(int count)
        {
            _select.LimitInternal(count);
            return _select;
        }

        /// <summary>
        /// Установить OFFSET. Возвращает <see cref="PagingItem"/> для fluent-цепочки.
        /// </summary>
        public PagingItem Offset(int offset)
        {
            _select.OffsetInternal(offset);
            return this;
        }

        /// <summary>
        /// Установить OFFSET и вернуть <see cref="Select"/>.
        /// </summary>
        public Select Skip(int count)
        {
            _select.OffsetInternal(count);
            return _select;
        }

        /// <summary>
        /// Установить страницу (LIMIT + OFFSET). Возвращает <see cref="Select"/>.
        /// </summary>
        public Select Page(int page, int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }

            _select.LimitInternal(pageSize);
            _select.OffsetInternal((page - 1) * pageSize);

            return _select;
        }
    }
}
