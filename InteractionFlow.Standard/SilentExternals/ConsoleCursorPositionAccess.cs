using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternals;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SilentExternals
{
    public class ConsoleCursorPositionAccess : SilentRequest<ConsoleCursorPosition, ConsoleCursorPosition>, IConsoleCursorPositionAccess
    {
        public ConsoleCursorPosition Position
        {
            get => new(Console.CursorLeft, Console.CursorTop);
            set
            {
                if (value.Left.HasValue)
                    Console.CursorLeft = value.Left.Value;

                if (value.Top.HasValue)
                    Console.CursorTop = value.Top.Value;
            }
        }

        public override ValueTask<ConsoleCursorPosition> ExecuteAsync(IFlowContext context, ConsoleCursorPosition arguments)
        {
            Position = arguments;
            return new(Position);
        }

        public override void ForceResetMemoryState()
        {
        }
    }
}
