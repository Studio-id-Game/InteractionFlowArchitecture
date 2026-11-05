using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts.OperationPorts
{
    /// <summary>標準の行編集を使用して入力を受け取ります。</summary>
    public interface IConsoleLineInputPort : IOperationPort
    {
        /// <summary>一行を取得します。空行は空文字、入力終端は null です。</summary>
        /// <remarks>
        /// キャンセル時も開始済みの読み取り終了を待つため、Enter が必要な場合があります。
        /// 標準実装は読み取り成功後にも受付猶予を設け、その終了時に正常完了を確定します。
        /// </remarks>
        ValueTask<string?> ReadLineAsync(IFlowContext context);
    }
}
