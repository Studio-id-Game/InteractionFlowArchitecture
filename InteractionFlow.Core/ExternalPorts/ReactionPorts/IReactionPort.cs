using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;

namespace InteractionFlow.Core.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// ユーザーに観測可能な出力や終了時の反応を担当する Reaction ポートを表します。
    /// </summary>
    public interface IReactionPort : IFlowNodeStateful
    {
        /// <summary>
        /// Reaction ポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが Reaction 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        /// <summary>
        /// Reaction が決定したフロー終了結果を生成します。
        /// </summary>
        /// <param name="exception">Reaction が未解決として扱う例外。解決済みの場合は <see langword="null"/>。</param>
        /// <returns>Reaction が生成したフロー終了結果。</returns>
        protected static ReactionEnd GetEnd(Exception? exception = null)
        {
            return exception == null ? ReactionEnd.Success : new(exception);
        }
    }
}
