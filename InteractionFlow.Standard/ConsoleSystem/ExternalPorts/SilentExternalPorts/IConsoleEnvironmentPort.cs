using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Entities;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts.SilentExternalPorts
{
    /// <summary>画面を変更せず、コンソールの実行環境を観測します。</summary>
    public interface IConsoleEnvironmentPort : ISilentExternalPort
    {
        /// <summary>標準入力がリダイレクトされているか。</summary>
        bool IsInputRedirected { get; }
        /// <summary>標準出力がリダイレクトされているか。</summary>
        bool IsOutputRedirected { get; }
        /// <summary>現在位置、または取得失敗を返します。</summary>
        Result<ConsolePosition> GetCursorPosition();
    }
}
