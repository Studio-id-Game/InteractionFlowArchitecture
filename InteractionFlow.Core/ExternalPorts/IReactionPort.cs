using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;

namespace InteractionFlow.Core.ExternalPorts
{
    /// <summary>
    /// System から User への観測可能な反応と、その反応に対応する Context への影響を表す Reaction ポートです。
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
            return GetEnd(exception is null ? Result.Success : (Result)exception);
        }

        /// <summary>Reaction が決定した処理結果から終了結果を生成します。</summary>
        /// <param name="result">Reaction の処理結果。</param>
        /// <returns>Reaction が生成した終了結果。</returns>
        protected static ReactionEnd GetEnd(Result result)
        {
            return result.Try(out _) ? ReactionEnd.Success : new(result);
        }
    }
}
