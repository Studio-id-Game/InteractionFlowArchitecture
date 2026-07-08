using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// Interaction 中に発生した例外を、フロー終了時の反応へ変換する Reaction ポートを表します。
    /// </summary>
    /// <typeparam name="T">このポートが扱う例外の型。</typeparam>
    public interface IExceptionPort<in T> : IReactionPort
        where T : Exception
    {
        /// <summary>
        /// 例外をハンドリングせず再送出するかどうかを取得または設定します。
        /// </summary>
        public bool ThrowException { get; set; }

        /// <summary>
        /// 例外ハンドリングポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが Reaction 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        /// <summary>
        /// 指定された例外を処理し、フロー終了トークンへ変換します。
        /// </summary>
        /// <param name="context">例外が発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理する例外。</param>
        /// <returns>例外処理後のフロー終了トークン。</returns>
        ValueTask<FlowEndToken> HandleExceptionAsync(IFlowContext context, T exception);
    }
}
