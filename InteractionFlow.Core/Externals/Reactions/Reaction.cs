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
        private readonly IFlowNode[] dependency;

        private FlowEndToken? lastFlowEndToken;

        /// <summary>
        /// 依存ノードを保持し、派生クラスの状態を初期化します。
        /// </summary>
        /// <param name="dependency">この Reaction が依存するフローノード。</param>
        public Reaction(params IFlowNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        /// <summary>
        /// この Reaction が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        /// <summary>
        /// 派生クラスが保持するメモリ上の状態を初期化します。
        /// </summary>
        public abstract void ForceResetMemoryState();

        /// <summary>
        /// 指定されたコンテキストに対応するフロー終了トークンを作成または再利用します。
        /// </summary>
        /// <param name="context">フロー終了時点のコンテキスト。</param>
        /// <returns>指定されたコンテキストに対応するフロー終了トークン。</returns>
        protected FlowEndToken CreateFlowEndToken(IFlowContext context)
        {
            if (lastFlowEndToken == null || lastFlowEndToken.LastContext != context)
            {
                return lastFlowEndToken = new(context);
            }
            else
            {
                lastFlowEndToken.Exception = null;
                return lastFlowEndToken;
            }
        }
    }
}
