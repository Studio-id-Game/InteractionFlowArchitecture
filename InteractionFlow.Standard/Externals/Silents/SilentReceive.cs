using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Silents
{
    public abstract class SilentReceive<TResult>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentReceivePort<TResult>
    {
        public abstract ValueTask<TResult> ExecuteAsync(IFlowContext context);
    }
}
