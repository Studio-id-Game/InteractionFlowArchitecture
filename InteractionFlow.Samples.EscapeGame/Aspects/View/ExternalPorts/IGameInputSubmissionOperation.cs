using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;

public interface IGameInputSubmissionOperation : IOperationPort
{
    bool TrySubmitCommand(IViewContext context, long sessionId, GameCommand command);
    bool TrySubmitControl(IViewContext context, GameControlInput input);
}
