using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.SilentPorts
{
    public interface ICancellationWithConsole : ISilentExternalPort
    {
        public ValueTask Setup(IFlowContext context);
    }
}
