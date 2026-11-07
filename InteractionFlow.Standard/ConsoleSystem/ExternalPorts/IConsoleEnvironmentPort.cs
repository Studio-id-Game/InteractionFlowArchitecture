using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Entities;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts
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
        /// <summary>表示ウィンドウの列数・行数、または取得失敗を返します。リダイレクト出力は非対応です。</summary>
        Result<(int Width, int Height)> GetWindowSize();
    }
}
