using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;

public interface IWindowOperation : IOperationPort
{
    void PollFrameInput(IViewContext context);
}
