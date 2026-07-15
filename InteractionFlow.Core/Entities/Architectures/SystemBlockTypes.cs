namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// InteractionFlow が Architecture として定義する、システム上のブロック種別を表します。
    /// </summary>
    /// <remarks>
    /// この列挙体は、Analyzer、可視化、設計検査などが共通して参照できる Architecture 概念の分類語彙です。
    /// ユーザー拡張点そのものではなく、InteractionFlow が定義する安定した分類を表します。
    /// 値の追加や変更は、Architecture 概念の追加や見直しに伴って行われます。
    /// </remarks>
    public enum SystemBlockTypes
    {
        /// <summary>
        /// Architecture 上のブロック種別が指定されていない状態です。
        /// </summary>
        None = 0,

        /// <summary>
        /// ドメインモデルや業務概念など、対象領域の中心となるブロックです。
        /// </summary>
        Domain,

        /// <summary>
        /// Interaction、FunctionPort、External など、責務を分離するレイヤー構造を表すブロックです。
        /// </summary>
        Layers,

        /// <summary>
        /// 外部システム、外部機能、入出力先など、フローの外側と接続するブロックです。
        /// </summary>
        External,

        /// <summary>
        /// SystemFlow の構築や依存関係の結線を担当するビルダー領域を表すブロックです。
        /// </summary>
        SystemFlowBuilder,
    }
}
