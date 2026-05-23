using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.SilentPorts
{
    public interface ISilentReceivePort<TResult> : ISilentExternalPort
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        public ValueTask<TResult> ExecuteAsync(IFlowContext context);
    }
}
