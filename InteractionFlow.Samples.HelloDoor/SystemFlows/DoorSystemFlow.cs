using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.SystemFlows
{
    public sealed class DoorSystemFlow(OperateDoor operateDoor)
        : SystemFlow<IFlowContext>(operateDoor)
    {
        protected override async Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            InteractionEnd end;

            while (true)
            {
                end = await operateDoor.ExecuteAsync(context);

                if (end.HasException)
                {
                    break;
                }

                if (context.GetRequired<DoorState>().ExitRequested)
                {
                    break;
                }
            }

            return end;
        }
    }
}
