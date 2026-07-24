namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// FunctionPort レイヤーに属するノードの機能種別を表します。
    /// </summary>
    public enum FunctionPortTypes
    {
        /// <summary>
        /// FunctionPort 種別が指定されていない状態です。
        /// </summary>
        None = 0,

        /// <summary>
        /// User による操作や入力の取得を担当する Operation ポートです。
        /// </summary>
        Operation,

        /// <summary>
        /// ユーザーに観測可能な出力や終了時の反応を担当する Reaction ポートです。
        /// </summary>
        Reaction,

        /// <summary>
        /// ユーザーに直接観測されない、外部実行環境とのやりとりを担当する SilentExternal ポートです。
        /// </summary>
        SilentExternal,

        /// <summary>
        /// 状態の保存・読み込み・管理を担当する Storage ポートです。
        /// </summary>
        Storage,
    }
}
