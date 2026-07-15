using System;

namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// 依存グラフに参加できる最小単位を表します。
    /// </summary>
    public interface IDependencyNode
    {
        /// <summary>
        /// このノードが依存する他のノードを取得します。
        /// </summary>
        ReadOnlySpan<IDependencyNode> Dependency { get; }
    }
}
