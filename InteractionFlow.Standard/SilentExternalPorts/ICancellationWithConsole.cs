using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SilentExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.SilentExternalPorts
{
    public interface ICancellationWithConsole : ISilentExternalPort
    {
        public ValueTask Setup(IFlowContext context);
    }
}
