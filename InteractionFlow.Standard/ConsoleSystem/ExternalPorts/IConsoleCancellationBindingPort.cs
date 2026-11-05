using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts
{
    /// <summary>Ctrl+C をフローのキャンセル要求へ接続します。</summary>
    public interface IConsoleCancellationBindingPort : ISilentExternalPort
    {
        /// <summary>キャンセル対象を接続します。同じ対象への再接続は何もしません。</summary>
        /// <remarks>接続中の対象変更と、別インスタンスからの同時接続は拒否します。</remarks>
        void Bind(IFlowContext context);
        /// <summary>イベント購読と接続先を解除します。対象の所有権は取得しません。</summary>
        void Unbind();
    }
}
