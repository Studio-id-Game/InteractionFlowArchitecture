using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// Interaction 中に発生したキャンセルを、フロー終了時の反応へ変換する Reaction ポートを表します。
    /// </summary>
    public interface ICancellationPort : IReactionPort, IExceptionPort<OperationCanceledException>
    {
        /// <summary>
        /// キャンセルハンドリングポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが Reaction 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        /// <summary>
        /// 指定されたキャンセル例外を処理し、フロー終了結果へ変換します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        ValueTask<ReactionEnd> HandleCancellationAsync(IFlowContext context, OperationCanceledException exception);

        /// <summary>
        /// 例外ハンドリング契約から呼び出されたキャンセル例外を、キャンセル専用の処理へ委譲します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        ValueTask<ReactionEnd> IExceptionPort<OperationCanceledException>.HandleExceptionAsync(IFlowContext context, OperationCanceledException exception)
        {
            return HandleCancellationAsync(context, exception);
        }
    }
}
