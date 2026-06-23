using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{
    public abstract class SilentSend<TArg>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentSendPort<TArg>
    {
        public abstract ValueTask ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
