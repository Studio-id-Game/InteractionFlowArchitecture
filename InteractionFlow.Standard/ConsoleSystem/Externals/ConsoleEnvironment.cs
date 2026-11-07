using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>コンソールのリダイレクト状態、カーソル位置、表示ウィンドウのサイズを取得します。</summary>
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
        public Result<(int Width, int Height)> GetWindowSize()
        {
            lock (ConsoleSynchronization.OutputGate)
            {
                try
                {
                    if (Console.IsOutputRedirected)
                        throw new NotSupportedException("Console screen operations require a terminal.");

                    return (Console.WindowWidth, Console.WindowHeight).AsResult();
                }
                catch (Exception e)
                {
                    return e;
                }
            }
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
