using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{
    public abstract class SilentRequest<TResult, TArg>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentRequestPort<TResult, TArg>
    {
        public abstract ValueTask<TResult> ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
