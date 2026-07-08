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
        void ForceResetMemoryState();
    }
}
