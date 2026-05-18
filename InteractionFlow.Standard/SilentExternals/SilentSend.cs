using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternals;
using InteractionFlow.Standard.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SilentExternals
{
    public abstract class SilentSend<TArg>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentSendPort<TArg>
    {
        public abstract ValueTask ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
