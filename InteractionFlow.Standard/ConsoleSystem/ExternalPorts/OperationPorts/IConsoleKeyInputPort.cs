using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts.OperationPorts
{
    /// <summary>表示を伴わないキー入力を受け取ります。</summary>
    public interface IConsoleKeyInputPort : IOperationPort
    {
        /// <summary>キャンセル可能な待機で一キーを取得します。リダイレクト入力は非対応です。</summary>
        ValueTask<ConsoleKeyInfo> ReadKeyAsync(IFlowContext context);
    }
}
