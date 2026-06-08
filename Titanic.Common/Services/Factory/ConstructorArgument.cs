namespace Titanic.Common.Services.Factory
{
    /// <summary>
    /// Аргумент конструктора, передаваемый в ClassFactory по имени параметра.
    /// </summary>
    public sealed class ConstructorArgument
    {
        #region Members

        /// <summary>
        /// Создать аргумент конструктора.
        /// </summary>
        /// <param name="name"> Имя параметра конструктора. </param>
        /// <param name="value"> Значение аргумента. </param>
        public ConstructorArgument(string name, object? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Constructor argument name is empty.", nameof(name));
            }

            Name = name;
            Value = value;
        }

        /// <summary>
        /// Имя параметра конструктора.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Значение параметра конструктора.
        /// </summary>
        public object? Value { get; }

        #endregion Members
    }
}
