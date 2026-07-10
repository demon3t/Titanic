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
        /// Создать атрибут с именем подключения, которое будет использоваться при регистрации базы.
        /// </summary>
        /// <param name="name">Имя подключения из конфигурации или менеджера подключений.</param>
        public DatabaseConnectionAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Connection name is empty", nameof(name));

            Name = name;
        }
    }
}
