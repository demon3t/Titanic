namespace Titanic.Db.Enums
{
    /// <summary>
    /// Тип SQL JOIN.
    /// </summary>
    public enum JoinType
    {
        Inner = 0,
        Left = 1,
        Right = 2,
        Full = 3,
        Cross = 4
    }
}