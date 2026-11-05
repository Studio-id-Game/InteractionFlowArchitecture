using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>出力と排他しながら画面を変更します。</summary>
    public sealed class ConsoleScreen : Reaction, IConsoleScreenPort
    {
        /// <summary>端末の状態は変更しません。</summary>
        public override void ForceResetMemoryState()
        {
        }

        /// <inheritdoc/>
        public ValueTask<ReactionEnd> ClearAsync(IFlowContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            lock (ConsoleSynchronization.OutputGate)
            {
                try
                {
                    RequireScreen();
                    Console.Clear();
                    return new(GetEnd());
                }
                catch (Exception e)
                {
                    return new(GetEnd(e));
                }
            }
        }

        /// <inheritdoc/>
        public ValueTask<ReactionEnd> SetCursorPositionAsync(IFlowContext context, ConsolePosition position)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            lock (ConsoleSynchronization.OutputGate)
            {
                try
                {
                    RequireScreen();
                    Console.SetCursorPosition(position.Left, position.Top);
                    return new(GetEnd());
                }
                catch (Exception e)
                {
                    return new(GetEnd(e));
                }
            }
        }

        private static void RequireScreen()
        {
            if (Console.IsOutputRedirected)
                throw new NotSupportedException("Console screen operations require a terminal.");
        }
    }
}
