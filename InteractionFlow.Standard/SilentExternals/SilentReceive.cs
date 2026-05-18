using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternals;
using InteractionFlow.Standard.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SilentExternals
{
    public abstract class SilentReceive<TResult>(params IFlowNode[] dependency) : SilentExternal(dependency), ISilentReceivePort<TResult>
    {
        public abstract ValueTask<TResult> ExecuteAsync(IFlowContext context);
    }
}
