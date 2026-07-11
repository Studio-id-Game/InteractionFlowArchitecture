using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ProgramFlows
{
    /// <summary>
    /// ユーザーの目的を達成するための ProgramFlow を表します。
    /// </summary>
    public interface IProgramFlow : IFlowNode
    {
        /// <summary>
        /// ProgramFlow が ProgramFlow レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.ProgramFlow;

        /// <summary>
        /// ProgramFlow は FunctionPort 種別を持たないことを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        /// <summary>
        /// Interaction の終了トークンを、ProgramFlow に渡されたコンテキストへ結合し直します。
        /// </summary>
        /// <param name="context">ProgramFlow に渡されたフローコンテキスト。</param>
        /// <param name="interactionEnd">ProgramFlow 内の Interaction が返した終了トークン。</param>
        /// <returns>ProgramFlow の終了トークン。</returns>
        protected static FlowEndToken GetEnd(IFlowContext context, FlowEndToken interactionEnd)
        {
            return new(context, interactionEnd.End);
        }
    }

    /// <summary>
    /// 指定されたコンテキスト型で実行される ProgramFlow を表します。
    /// </summary>
    /// <typeparam name="TContext">ProgramFlow が扱うコンテキストの型。</typeparam>
    public interface IProgramFlow<in TContext> : IProgramFlow
        where TContext : IFlowContext
    {
        /// <summary>
        /// ProgramFlow が ProgramFlow レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.ProgramFlow;

        /// <summary>
        /// ProgramFlow は FunctionPort 種別を持たないことを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        /// <summary>
        /// 指定されたコンテキストで ProgramFlow を実行します。
        /// </summary>
        /// <param name="context">ProgramFlow に渡すコンテキスト。</param>
        /// <returns>ProgramFlow の終了結果。</returns>
        Task<FlowEndToken> ExecuteAsync(TContext context);
    }
}
