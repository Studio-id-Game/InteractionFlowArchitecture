using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    /// <summary>
    /// システム内部の目的を達成するための Interaction を表します。
    /// </summary>
    public interface IInteraction : IFlowNode
    {
        /// <summary>
        /// Interaction が Interaction レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Interaction;

        /// <summary>
        /// Interaction は FunctionPort 種別を持たないことを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        /// <summary>
        /// 指定されたコンテキストで Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <returns>Interaction の終了結果。</returns>
        Task<FlowEndToken> ExecuteAsync(IFlowContext context);
    }
}
