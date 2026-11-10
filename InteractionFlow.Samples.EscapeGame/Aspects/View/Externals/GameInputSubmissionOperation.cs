using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;

public sealed class GameInputSubmissionOperation : Operation, IGameInputSubmissionOperation
{
    public override void ForceResetMemoryState() { }

    public bool TrySubmitCommand(IViewContext context, long sessionId, GameCommand command)
        => context.View.TrySubmitCommand(sessionId, command);

    public bool TrySubmitControl(IViewContext context, GameControlInput input)
        => context.View.TrySubmitControl(input);
}
