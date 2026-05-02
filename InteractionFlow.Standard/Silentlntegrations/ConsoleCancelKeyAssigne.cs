using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Silentlntegrations;
using InteractionFlow.Standard.SilentlntegrationPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Silentlntegrations
{

    public class ConsoleCancelKeyAssigne : SilentIntegration<object?>, ICancelKeyAssigne
    {
        public override ValueTask IntegrateWithExternalAsync(IFlowContext context, object? arguments)
        {
            Console.CancelKeyPress += CancelKeyPress;
            return default;

            void CancelKeyPress(object? sender, ConsoleCancelEventArgs args)
            {
                if (context.Cancellation.HasTask)
                {
                    context.Cancellation.Cancel();
                }
                args.Cancel = true;
            }
        }
    }
}
