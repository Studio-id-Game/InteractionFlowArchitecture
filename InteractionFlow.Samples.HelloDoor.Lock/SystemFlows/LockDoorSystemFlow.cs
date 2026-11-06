using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.HelloDoor.Interactions;
using InteractionFlow.Samples.HelloDoor.Lock.Entities;
using InteractionFlow.Samples.HelloDoor.SystemFlows;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Lock.SystemFlows
{
    public sealed class LockDoorSystemFlow(OperateDoor operateDoor, DoorSystemFlow doorSystem) : SystemFlow<IFlowContext>(operateDoor, doorSystem)
    {
        protected override async Task<FlowEndToken> ExecuteCoreAsync(IFlowContext context)
        {
            using var lockContext = new ScopedFlowContext(context)
                // OperationとReactionの間で契約を変更せずに値を受け渡しするための状態を、このスコープでのみ追加
                .With(new RefEntry<DoorLockCommand>(DoorLockCommand.Unknown));

            return await doorSystem.ExecuteAsync(lockContext);
        }
    }
}
