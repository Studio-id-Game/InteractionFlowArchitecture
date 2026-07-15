namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// システム構成図などで扱う大まかなブロック種別を表します。
    /// </summary>
    public enum SystemBlockTypes
    {
        /// <summary>
        /// ブロック種別が指定されていない状態です。
        /// </summary>
        None = 0,

        /// <summary>
        /// ドメイン領域を表すブロックです。
        /// </summary>
        Domain,

        /// <summary>
        /// レイヤー構造を表すブロックです。
        /// </summary>
        Layers,

        /// <summary>
        /// 外部システムや外部機能を表すブロックです。
        /// </summary>
        External,

        /// <summary>
        /// SystemFlow を生成するビルダー領域を表すブロックです。
        /// </summary>
        SystemFlowBuilder,
    }
}
