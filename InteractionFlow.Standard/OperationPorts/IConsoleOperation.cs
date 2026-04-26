using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.OperationPorts;
using InteractionFlow.Standard.Entities.Consoles;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.OperationPorts
{
    public interface IConsoleOperation :
        IOperationPort<ConsoleInputText>,
        IOperationPort<ConsoleInputKeyInfo>,
        IOperationPort<ConsoleInputAnyKey>
    {
        public interface IDummy : IConsoleOperation,
            IValueOperation<ConsoleInputAnyKey>,
            IValueOperation<ConsoleInputKeyInfo>,
            IValueOperation<ConsoleInputText>
        {
            ConsoleInputText Text { get; set; }

            ConsoleInputKeyInfo KeyInfo { get; set; }

            int DelayTime { get; set; }
        }

        ConsoleState State { get; set; }

        ValueTask<ConsoleInputText> UserOperateTextAsync(IFlowContext context);

        ValueTask<ConsoleInputKeyInfo> UserOperateKeyInfoAsync(IFlowContext context);

        ValueTask<ConsoleInputAnyKey> UserOperateAnyKeyAsync(IFlowContext context);

        ValueTask<ConsoleInputText> IOperationPort<ConsoleInputText>.UserOperateAsync(IFlowContext context)
        {
            return UserOperateTextAsync(context);
        }


        ValueTask<ConsoleInputKeyInfo> IOperationPort<ConsoleInputKeyInfo>.UserOperateAsync(IFlowContext context)
        {
            return UserOperateKeyInfoAsync(context);
        }

        ValueTask<ConsoleInputAnyKey> IOperationPort<ConsoleInputAnyKey>.UserOperateAsync(IFlowContext context)
        {
            return UserOperateAnyKeyAsync(context);
        }
    }
}