using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Application.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Application.Externals;

public sealed class GameInputOperation : Operation, IGameInputOperation
{
    public override void ForceResetMemoryState() { }

    public ValueTask<GameCommand> ReadCommandAsync(IGameplayContext context)
        => context.Gameplay.ReadCommandAsync(context.SessionId, context.Cancellation.GetToken());

    public bool TrySubmitCommand(IGameViewContext context, long sessionId, GameCommand command)
        => context.View.TrySubmitCommand(sessionId, command);
}
