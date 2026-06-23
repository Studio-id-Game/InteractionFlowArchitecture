using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{
    public class ConsoleColorAccess : SilentRequest<(ConsoleColor foreground, ConsoleColor background), (ConsoleColor? foreground, ConsoleColor? background)>, IConsoleColorAccess
    {
        public ConsoleColor ForegroundColor
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        public ConsoleColor BackgroundColor
        {
            get => Console.BackgroundColor;
            set => Console.BackgroundColor = value;
        }

        public override ValueTask<(ConsoleColor foreground, ConsoleColor background)> ExecuteAsync(IFlowContext context, (ConsoleColor? foreground, ConsoleColor? background) arguments)
        {
            var (foreground, background) = arguments;

            if (foreground != null)
                ForegroundColor = foreground.Value;
            if (background != null)
                BackgroundColor = background.Value;

            return new((ForegroundColor, BackgroundColor));
        }

        public override void ForceResetMemoryState()
        {
        }
    }
}
