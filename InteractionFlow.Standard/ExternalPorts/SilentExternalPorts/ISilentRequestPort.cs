using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.SilentExternalPorts
{
    /// <summary>
    /// 引数を受け取り、ユーザーに直接見えない値を返す SilentExternal ポートを表します。
    /// </summary>
    /// <typeparam name="TResult">返す値の型。</typeparam>
    /// <typeparam name="TArg">実行に渡す引数の型。</typeparam>
    public interface ISilentRequestPort<TResult, in TArg> : ISilentExternalPort
    {
        /// <summary>
        /// SilentRequest ポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが SilentExternal 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        /// <summary>
        /// 指定された引数で処理を実行し、結果を返します。
        /// </summary>
        /// <param name="context">実行時のフローコンテキスト。</param>
        /// <param name="arguments">実行に渡す引数。</param>
        /// <returns>実行結果。</returns>
        public ValueTask<TResult> ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
