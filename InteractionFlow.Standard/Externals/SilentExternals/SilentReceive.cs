using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{
    public abstract class SilentReceive<TResult>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentReceivePort<TResult>
    {
        public abstract ValueTask<TResult> ExecuteAsync(IFlowContext context);
    }
}
