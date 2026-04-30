using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.SilentlntegrationPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Silentlntegrations
{
    public abstract class SilentIntegration<TArg> : ISilentIntegrationPort<TArg>
    {
        public abstract ValueTask IntegrateWithExternalAsync(IFlowContext context, TArg arguments);

        public virtual void ForceResetMemoryState()
        {
        }
    }
}
