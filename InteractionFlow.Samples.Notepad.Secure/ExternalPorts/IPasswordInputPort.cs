using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts
{
    public interface IPasswordInputPort : IOperationPort
    {
        Task<SecretPassword?> ReadAsync(IFlowContext context);
    }
}
