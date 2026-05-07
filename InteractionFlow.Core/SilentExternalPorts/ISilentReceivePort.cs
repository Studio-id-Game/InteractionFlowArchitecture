using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.SilentExternalPorts
{
    public interface ISilentReceivePort<TResult> : ISilentExternalPort
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        public ValueTask<TResult> ExecuteAsync(IFlowContext context);
    }
}
