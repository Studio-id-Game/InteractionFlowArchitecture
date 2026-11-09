using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameOperation : Operation, IGameOperation
{
    public override void ForceResetMemoryState() { }

    public ValueTask<GameCommand> ReadAsync(EscapeApplicationContext context)
        => context.Input.ReadAsync(context.SessionId, context.Cancellation.GetToken());

    public bool Submit(WindowContext context, long sessionId, GameCommand command)
        => context.Input.TrySubmit(sessionId, command);
}
