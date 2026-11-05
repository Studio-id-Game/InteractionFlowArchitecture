using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Old.Console.ExternalPorts.SilentExternalPorts
{
    /// <summary>
    /// コンソールのキャンセル入力をフローコンテキストのキャンセル制御へ接続する SilentExternal ポートを表します。
    /// </summary>
    public interface ICancellationWithConsole : ISilentExternalPort
    {
        /// <summary>
        /// 指定されたコンテキストに対してコンソールキャンセル連携を設定します。
        /// </summary>
        /// <param name="context">キャンセル連携を設定するフローコンテキスト。</param>
        /// <returns>設定処理の完了を表す値。</returns>
        public ValueTask Setup(IFlowContext context);
    }
}
