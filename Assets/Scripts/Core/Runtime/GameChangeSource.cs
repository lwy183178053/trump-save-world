namespace Core.Runtime
{
    /// <summary>
    /// 这次数据变化是由谁造成的。
    /// 这个字段不是为了算数值，而是为了给 UI、日志、音效、演出判断来源。
    /// </summary>
    public enum GameChangeSource
    {
        Unknown = 0,
        System = 1,
        PlayerAction = 2,
        UI = 3,
        Gift = 4,
        Proposal = 5,
        Event = 6,
        Restore = 7,
        Replay = 8,
        Debug = 9
    }
}
