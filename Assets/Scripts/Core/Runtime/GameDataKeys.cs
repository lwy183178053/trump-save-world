namespace Core.Table
{
    /// <summary>
    /// 这里放“读表后会用到的稳定键名”。
    ///
    /// 它和 Runtime 不同：
    /// - Runtime：负责存值、改值、广播事件
    /// - Table：负责定义策划表里会引用的 ID / Key
    ///
    /// 如果你以后把 Excel 导成 JSON 或 ScriptableObject，
    /// 这些键可以直接和表内容对应起来。
    /// </summary>
    public static class GameDataKeys
    {
        public const string SupportRate = "A_1";
        public const string Deterrence = "D_1";
        public const string HeartRate = "H_1";
    }
}
