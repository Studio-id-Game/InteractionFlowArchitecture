using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Console.Externals.SilentExternals
{
    /// <summary>
    /// 引数を受け取り、外部実行環境から値を返す SilentExternal 実装の基底クラスです。
    /// </summary>
    /// <typeparam name="TResult">返す値の型。</typeparam>
    /// <typeparam name="TArg">実行に渡す引数の型。</typeparam>
    /// <param name="dependency">この SilentExternal が依存するフローノード。</param>
    public abstract class SilentRequest<TResult, TArg>(params IDependencyNode[] dependency) : SilentExternal(dependency), ISilentRequestPort<TResult, TArg>
    {
        /// <summary>
        /// 指定された引数で処理を実行し、結果を返します。
        /// </summary>
        /// <param name="context">実行時のフローコンテキスト。</param>
        /// <param name="arguments">実行に渡す引数。</param>
        /// <returns>実行結果。</returns>
        public abstract ValueTask<TResult> ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
