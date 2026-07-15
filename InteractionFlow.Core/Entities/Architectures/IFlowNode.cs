using System;

namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// SystemFlow、Interaction、FunctionPort など、フローを構成するノードを表します。
    /// </summary>
    public interface IFlowNode
    {
        /// <summary>
        /// ノードの表示名を取得します。既定では実行時の型名を返します。
        /// </summary>
        string Name => GetType().Name;

        /// <summary>
        /// ノードが属するフロー上のレイヤーを取得します。
        /// </summary>
        FlowLayerTypes Layer { get; }

        /// <summary>
        /// FunctionPort レイヤー内での機能種別を取得します。
        /// </summary>
        FunctionPortTypes FunctionTypes { get; }

        /// <summary>
        /// このノードが依存する他のノードを取得します。
        /// </summary>
        ReadOnlySpan<IFlowNode> Dependency { get; }
    }
}
