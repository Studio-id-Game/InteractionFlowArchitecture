using InteractionFlow.Core.Entities.Contexts;
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

        public int CancelWaitTime { get; set; }

        ValueTask<ConsoleInputText> UserOperateTextAsync(IFlowContext context);

        ValueTask<ConsoleInputKeyInfo> UserOperateKeyInfoAsync(IFlowContext context);

        ValueTask<ConsoleInputKeyInfo> UserOperateKeyInfoAsync(IFlowContext context, bool hideChar);

        ValueTask<ConsoleInputAnyKey> UserOperateAnyKeyAsync(IFlowContext context);

        ValueTask<ConsoleInputText> IOperationPort<ConsoleInputText>.OperateFromUserAsync(IFlowContext context)
        {
            return UserOperateTextAsync(context);
        }


        ValueTask<ConsoleInputKeyInfo> IOperationPort<ConsoleInputKeyInfo>.OperateFromUserAsync(IFlowContext context)
        {
            return UserOperateKeyInfoAsync(context);
        }

        ValueTask<ConsoleInputAnyKey> IOperationPort<ConsoleInputAnyKey>.OperateFromUserAsync(IFlowContext context)
        {
            return UserOperateAnyKeyAsync(context);
        }
    }
}
