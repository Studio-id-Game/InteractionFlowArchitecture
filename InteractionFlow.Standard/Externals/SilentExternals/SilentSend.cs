using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Silents
{
    public abstract class SilentSend<TArg>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentSendPort<TArg>
    {
        public abstract ValueTask ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
