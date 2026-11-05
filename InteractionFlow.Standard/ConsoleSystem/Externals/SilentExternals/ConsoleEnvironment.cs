using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.SilentExternalPorts;
using System;

namespace InteractionFlow.Standard.ConsoleSystem.Externals.SilentExternals
{
    /// <summary>コンソールのリダイレクト状態とカーソル位置を取得します。</summary>
    public sealed class ConsoleEnvironment : SilentExternal, IConsoleEnvironmentPort
    {
        /// <inheritdoc/>
        public bool IsInputRedirected => Console.IsInputRedirected;

        /// <inheritdoc/>
        public bool IsOutputRedirected => Console.IsOutputRedirected;

        /// <inheritdoc/>
        public override void ForceResetMemoryState()
        {
        }

        /// <inheritdoc/>
        public Result<ConsolePosition> GetCursorPosition()
        {
            lock (ConsoleSynchronization.OutputGate)
            {
                try
                {
                    if (Console.IsOutputRedirected)
                        throw new NotSupportedException("Console screen operations require a terminal.");

                    return new ConsolePosition(Console.CursorLeft, Console.CursorTop);
                }
                catch (Exception e)
                {
                    return e;
                }
            }
        }
    }
}
