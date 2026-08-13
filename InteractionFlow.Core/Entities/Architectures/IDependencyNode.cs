using System;

namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// 依存グラフに参加できる最小単位を表します。
    /// </summary>
    public interface IDependencyNode
    {
        /// <summary>
        /// ノードの表示名を取得します。既定では実行時の型名を返します。
        /// </summary>
        string Name => GetType().Name;

        /// <summary>
        /// このノードが依存する他のノードを取得します。
        /// </summary>
        ReadOnlyMemory<IDependencyNode> Dependency { get; }
    }
}
