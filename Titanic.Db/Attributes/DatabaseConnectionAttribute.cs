using System;

namespace Titanic.Db.Attributes
{
    /// <summary>
    /// Атрибут для регистрации класса-обёртки подключения по имени.
    /// Позволяет найти нужное подключение в <see cref="Titanic.Db.DbManager"/> по строковому имени,
    /// без явной ссылки на конфиг.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class DatabaseConnectionAttribute : Attribute
    {
        /// <summary>
        /// Имя подключения, по которому <see cref="Titanic.Db.DbManager"/> найдёт нужное подключение.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseConnectionAttribute"/> class.
        /// </summary>
        /// <param name="name">The configured database connection name.</param>
        public DatabaseConnectionAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Connection name is empty", nameof(name));

            Name = name;
        }
    }
}
