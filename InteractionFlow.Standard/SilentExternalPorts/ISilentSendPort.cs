using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SilentExternalPorts
{
    public interface ISilentSendPort<in TArg> : ISilentExternalPort
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        public ValueTask ExecuteAsync(IFlowContext context, TArg arguments);
    }
}
