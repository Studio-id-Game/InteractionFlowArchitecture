using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IWindowOperation : IOperationPort
{
    void Poll(IGameViewContext context);
}
