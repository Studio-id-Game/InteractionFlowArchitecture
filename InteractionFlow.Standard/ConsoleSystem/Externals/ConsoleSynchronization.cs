namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    // 出力、画面変更、位置取得は同じ端末を扱うため、Scope をまたいで同期する。
    internal static class ConsoleSynchronization
    {
        internal static object OutputGate { get; } = new();
    }
}
