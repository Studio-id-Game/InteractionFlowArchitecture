using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System;

namespace InteractionFlow.Core.Externals.Reactions
{
    /// <summary>
    /// Reaction ポートのデフォルト実装基底クラスです。
    /// </summary>
    public abstract class Reaction : IReactionPort
    {
        private readonly IDependencyNode[] dependency;

        /// <summary>
        /// 依存ノードを保持し、派生クラスの状態を初期化します。
        /// </summary>
        /// <param name="dependency">この Reaction が依存するフローノード。</param>
        public Reaction(params IDependencyNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        /// <summary>
        /// この Reaction が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IDependencyNode> Dependency => dependency;

        /// <summary>
        /// 派生クラスが保持するメモリ上の状態を初期化します。
        /// </summary>
        public abstract void ForceResetMemoryState();

        /// <summary>
        /// Reaction が決定したフロー終了結果を生成します。
        /// </summary>
        /// <param name="exception">Reaction が未解決として扱う例外。解決済みの場合は <see langword="null"/>。</param>
        /// <returns>Reaction が生成したフロー終了結果。</returns>
        protected static ReactionEnd GetEnd(Exception? exception = null)
        {
            return IReactionPort.GetEnd(exception);
        }
    }
}
