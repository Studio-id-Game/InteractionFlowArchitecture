using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.SystemFlows
{
    /// <summary>
    /// User への反応プロセスとして Interaction を束ねた SystemFlow を表します。
    /// </summary>
    public interface ISystemFlow : IFlowNode
    {
        /// <summary>
        /// SystemFlow が SystemFlow レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.SystemFlow;

        /// <summary>
        /// SystemFlow は FunctionPort 種別を持たないことを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        /// <summary>
        /// Interaction の終了トークンを、SystemFlow に渡されたコンテキストへ結合し直します。
        /// </summary>
        /// <param name="context">SystemFlow に渡されたフローコンテキスト。</param>
        /// <param name="interactionEnd">SystemFlow 内の Interaction が返した終了トークン。</param>
        /// <returns>SystemFlow の終了トークン。</returns>
        protected static FlowEndToken GetEnd(IFlowContext context, FlowEndToken interactionEnd)
        {
            return new(context, interactionEnd.End);
        }
    }

    /// <summary>
    /// 指定されたコンテキスト型で実行される SystemFlow を表します。
    /// </summary>
    /// <typeparam name="TContext">SystemFlow が扱うコンテキストの型。</typeparam>
    public interface ISystemFlow<in TContext> : ISystemFlow
        where TContext : IFlowContext
    {
        /// <summary>
        /// SystemFlow が SystemFlow レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.SystemFlow;

        /// <summary>
        /// SystemFlow は FunctionPort 種別を持たないことを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        /// <summary>
        /// 指定されたコンテキストで SystemFlow を実行します。
        /// </summary>
        /// <param name="context">SystemFlow に渡すコンテキスト。</param>
        /// <returns>SystemFlow の終了結果。</returns>
        Task<FlowEndToken> ExecuteAsync(TContext context);
    }
}
