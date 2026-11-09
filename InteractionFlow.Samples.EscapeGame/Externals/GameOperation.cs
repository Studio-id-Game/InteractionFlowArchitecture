using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameOperation : Operation, IGameOperation
{
    public override void ForceResetMemoryState() { }

    public ValueTask<GameCommand> ReadAsync(IGameplayContext context)
        => context.Gameplay.ReadAsync(context.SessionId, context.Cancellation.GetToken());

    public bool Submit(IGameViewContext context, long sessionId, GameCommand command)
        => context.View.TrySubmit(sessionId, command);
}
