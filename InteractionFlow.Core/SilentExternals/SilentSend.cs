using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.SilentExternals
{
    public abstract class SilentSend<TArg>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentSendPort<TArg>
    {
        public abstract ValueTask ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
