using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.OperationPorts
{
    public interface IConsoleOperation : IOperationPort, IHasFunctionState<ConsoleOperationState>
    {
        public interface IDummy : IConsoleOperation
        {
            ConsoleInputText DummyText { get; set; }

            ConsoleInputKeyInfo DummyKeyInfo { get; set; }

            int InputDelayTime { get; set; }
        }

        ValueTask<ConsoleInputText> WaitUserTextAsync(IFlowContext context);

        ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context);

        ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context, bool hideChar);
    }
}
