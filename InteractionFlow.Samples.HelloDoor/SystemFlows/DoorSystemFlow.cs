using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.SystemFlows
{
    internal sealed class DoorSystemFlow(OperateDoor operateDoor)
        : SystemFlow<IFlowContext>(operateDoor)
    {
        protected override async Task<FlowEndToken> ExecuteCoreAsync(IFlowContext context)
        {
            FlowEndToken end;

            while (true)
            {
                end = await operateDoor.ExecuteAsync(context);

                if (context.TryGet<DoorState>(out var door) &&
                    door.ExitRequested)
                {
                    break;
                }
            }

            return end;
        }
    }
}
