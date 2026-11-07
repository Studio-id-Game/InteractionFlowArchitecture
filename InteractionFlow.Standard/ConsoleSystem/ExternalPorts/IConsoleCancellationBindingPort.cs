using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using System;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts
{
    /// <summary>Ctrl+C をフローのキャンセル要求へ接続します。</summary>
    public interface IConsoleCancellationBindingPort : ISilentExternalPort
    {
        /// <summary>接続を取得し、破棄時に参照を解放するスコープを返します。</summary>
        /// <remarks>
        /// 同じ Port インスタンス・同じ CancellationObject への接続を参照カウントで共有し、
        /// 最後のスコープの破棄で購読を解除します。接続中の対象変更と別インスタンスの同時接続は拒否します。
        /// </remarks>
        IDisposable Bind(IFlowContext context);
    }
}
