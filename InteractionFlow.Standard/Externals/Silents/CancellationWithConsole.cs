using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Silents
{
    public class CancellationWithConsole : SilentExternal, ICancellationWithConsole
    {
        ConsoleCancelEventHandler? cancelKeyPress;

        public ValueTask Setup(IFlowContext context)
        {
            if (cancelKeyPress != null)
            {
                Console.CancelKeyPress -= cancelKeyPress;
            }

            cancelKeyPress = (sender, args) =>
            {
                CancelKeyPress(context, args);
            };

            Console.CancelKeyPress += cancelKeyPress;

            return default;
        }

        public override void ForceResetMemoryState()
        {
            if (cancelKeyPress != null)
            {
                Console.CancelKeyPress -= cancelKeyPress;
            }
        }

        protected virtual void CancelKeyPress(IFlowContext context, ConsoleCancelEventArgs args)
        {
            if (context.Cancellation.HasTask)
            {
                context.Cancellation.Cancel();
            }
            args.Cancel = true;
        }
    }
}
