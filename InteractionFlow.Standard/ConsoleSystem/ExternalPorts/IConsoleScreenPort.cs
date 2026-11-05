using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts
{
    /// <summary>コンソール画面を変更します。</summary>
    public interface IConsoleScreenPort : IReactionPort
    {
        /// <summary>画面を消去します。非対応環境では失敗結果を返します。</summary>
        ValueTask<ReactionEnd> ClearAsync(IFlowContext context);
        /// <summary>カーソルを移動します。非対応環境や無効な位置では失敗結果を返します。</summary>
        ValueTask<ReactionEnd> SetCursorPositionAsync(IFlowContext context, ConsolePosition position);
    }
}
