using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IGameInputOperation : IOperationPort
{
    ValueTask<GameCommand> ReadCommandAsync(IGameplayContext context);
    bool TrySubmitCommand(IGameViewContext context, long sessionId, GameCommand command);
}
