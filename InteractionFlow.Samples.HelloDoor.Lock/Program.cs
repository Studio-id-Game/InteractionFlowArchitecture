using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts;
using InteractionFlow.Samples.HelloDoor.Interactions;
using InteractionFlow.Samples.HelloDoor.Lock.Entities;
using InteractionFlow.Samples.HelloDoor.Lock.Externals;
using InteractionFlow.Samples.HelloDoor.Lock.SystemFlows;
using InteractionFlow.Samples.HelloDoor.SystemFlows;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.Builders;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Lock
{
    internal static class Program
    {
        private static async Task Main()
        {
            var builder = new ScopeBuilder();

            // Port と External 実装、Interaction を DI に登録します。
            builder
                // Interaction の基底クラスが利用する例外/キャンセル表示 Port も登録します。
                .Apply(ConsoleProfile.Default)
                .UseFunction<IDoorOperation, ConsoleLockDoorOperation>()
                .UseFunction<IDoorReaction, ConsoleLockDoorReaction>()
                .UseInteraction<OperateDoor>()
                .UseSystemFlow<DoorSystemFlow>(); // カスタムの LockDoorSystemFlow が内部で利用する

            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<LockDoorSystemFlow, IFlowContext>();

            using var context = new FlowContext()
                .With(new DoorState { IsOpen = false })
                .With(new DoorLockState { IsLocked = true }); // Lock の状態を追加

            var end = await flow.ExecuteAsync(context);

            if (end.HasException)
            {
                Console.WriteLine(end.Exception);
            }
        }
    }
}
