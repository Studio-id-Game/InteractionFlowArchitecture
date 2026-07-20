using InteractionFlow.Standard.Console.Entities;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;

namespace InteractionFlow.Standard.Console.ExternalPorts.SilentExternalPorts
{
    /// <summary>
    /// 現在のコンソールカーソル位置を取得または変更する SilentExternal ポートを表します。
    /// </summary>
    public interface IConsoleCursorPositionAccess : ISilentRequestPort<ConsoleCursorPosition, ConsoleCursorPosition>
    {
        /// <summary>
        /// 現在のカーソル位置を取得または設定します。
        /// </summary>
        public ConsoleCursorPosition Position { get; set; }
    }
}
