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
        /// System から User への観測可能な反応と、その反応に対応する Context への影響を表す Reaction ポートです。
        /// </summary>
        Reaction,

        /// <summary>
        /// User との相互作用や System 内での記録を直接の目的とせず、外部実行環境と連携する SilentExternal ポートです。
        /// </summary>
        SilentExternal,

        /// <summary>
        /// 状態の保存・読み込み・管理を担当する Storage ポートです。
        /// </summary>
        Storage,
    }
}
