using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternals;
using InteractionFlow.Standard.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SilentExternals
{
    public abstract class SilentRequest<TResult, TArg>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentRequestPort<TResult, TArg>
    {
        public abstract ValueTask<TResult> ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
