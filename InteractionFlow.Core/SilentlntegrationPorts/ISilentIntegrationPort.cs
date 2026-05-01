using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.SilentlntegrationPorts
{
    public interface ISilentIntegrationPort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentIntegration;
    }

    public interface ISilentIntegrationPort<in TArg> : ISilentIntegrationPort
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentIntegration;

        public ValueTask IntegrateWithExternalAsync(IFlowContext context, TArg arguments);
    }
}
