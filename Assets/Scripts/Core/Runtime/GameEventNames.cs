namespace Core.Runtime
{
    /// <summary>
    /// 建议统一使用的事件名。
    /// 把字符串集中在这里，避免全项目到处手写拼错。
    /// </summary>
    public static class GameEventNames
    {
        public const string GameStarted = "game.started";
        public const string ButtonPressed = "button.pressed";
        public const string ProposalSubmitted = "proposal.submitted";
        public const string ProposalResolved = "proposal.resolved";
        public const string GiftReceived = "gift.received";
        public const string GameWon = "game.won";
        public const string GameLost = "game.lost";
        public const string ValueThresholdReached = "value.threshold_reached";
        public const string TwitterPosted = "twitter.posted";//刚QIE加的，QTE完成后触发的事件
    }
}
