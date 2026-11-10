using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Externals;

public sealed class GameInputOperation : Operation, IGameInputOperation
{
    public override void ForceResetMemoryState() { }

    public ValueTask<GameCommand> ReadCommandAsync(IGameplayContext context)
        => context.Gameplay.ReadCommandAsync(context.SessionId, context.Cancellation.GetToken());

    public bool TrySubmitCommand(IViewContext context, long sessionId, GameCommand command)
        => context.View.TrySubmitCommand(sessionId, command);
}
