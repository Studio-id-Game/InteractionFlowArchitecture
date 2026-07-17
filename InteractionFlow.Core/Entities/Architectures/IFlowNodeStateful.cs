namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// 実行中に内部状態を持ち、その状態を強制的に初期化できるフローノードを表します。
    /// </summary>
    public interface IFlowNodeStateful : IFlowNode
    {
        /// <summary>
        /// ノードが保持している内部状態を強制的に初期化します。
        /// </summary>
        /// <remarks>
        /// この操作は、通常の削除可否判定や利用中状態よりもリセットを優先するための強い操作です。
        /// 実行中の処理へ影響する可能性があるため、フローの再初期化や明示的な状態破棄が必要な場面で使用します。
        /// </remarks>
        void ForceResetMemoryState();
    }
}
