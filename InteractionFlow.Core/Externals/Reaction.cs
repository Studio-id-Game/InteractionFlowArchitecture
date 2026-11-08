using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using System;

namespace InteractionFlow.Core.Externals
{
    /// <summary>
    /// Reaction ポートのデフォルト実装基底クラスです。
    /// </summary>
    public abstract class Reaction : IReactionPort
    {
        private readonly IDependencyNode[] dependency;

        /// <summary>
        /// 依存ノードを保持します。派生クラスの状態初期化は派生クラスのコンストラクタで行います。
        /// </summary>
        /// <param name="dependency">この Reaction が依存するフローノード。</param>
        public Reaction(params IDependencyNode[] dependency)
        {
            this.dependency = dependency;
        }

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        /// <summary>
        /// この Reaction が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlyMemory<IDependencyNode> Dependency => dependency;

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
            return GetEnd(exception is null ? Result.Success : (Result)exception);
        }

        /// <summary>Reaction が決定した処理結果から終了結果を生成します。</summary>
        /// <param name="result">Reaction の処理結果。</param>
        /// <returns>Reaction が生成した終了結果。</returns>
        protected static ReactionEnd GetEnd(Result result)
        {
            return IReactionPort.GetEnd(result);
        }
    }
}
