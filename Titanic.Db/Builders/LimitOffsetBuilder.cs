namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер LIMIT/OFFSET.
    /// Позволяет задать лимит, смещение и страницу.
    /// </summary>
    /// <typeparam name="TParent"> Тип родительского билдера. </typeparam>
    internal class LimitOffsetBuilder<TParent>
    {
        private readonly TParent _parent;
        private readonly Action<int> _setLimit;
        private readonly Action<int> _setOffset;

        internal LimitOffsetBuilder(TParent parent, Action<int> setLimit, Action<int> setOffset)
        {
            _parent = parent;
            _setLimit = setLimit;
            _setOffset = setOffset;
        }

        /// <summary>
        /// Установить LIMIT.
        /// </summary>
        public LimitOffsetBuilder<TParent> Limit(int limit)
        {
            _setLimit(limit);
            return this;
        }

        /// <summary>
        /// Установить LIMIT (алиас).
        /// </summary>
        public LimitOffsetBuilder<TParent> Take(int count) => Limit(count);

        /// <summary>
        /// Установить OFFSET.
        /// </summary>
        public LimitOffsetBuilder<TParent> Offset(int offset)
        {
            _setOffset(offset);
            return this;
        }

        /// <summary>
        /// Установить OFFSET (алиас).
        /// </summary>
        public LimitOffsetBuilder<TParent> Skip(int count) => Offset(count);

        /// <summary>
        /// Установить страницу (LIMIT + OFFSET).
        /// </summary>
        public LimitOffsetBuilder<TParent> Page(int page, int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }

            _setLimit(pageSize);
            _setOffset((page - 1) * pageSize);
            return this;
        }

        /// <summary>
        /// Завершить контекст LIMIT/OFFSET и вернуть родительский билдер.
        /// </summary>
        public TParent End() => _parent;
    }

}
