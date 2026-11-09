using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.View.ExternalPorts;

public interface IWindowOperation : IOperationPort
{
    void PollFrameInput(IGameViewContext context);
}
