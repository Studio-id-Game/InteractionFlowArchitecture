using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.ExternalPorts;

public interface IGameInputOperation : IOperationPort
{
    ValueTask<GameCommand> ReadCommandAsync(IGameplayContext context);
    bool TrySubmitCommand(IViewContext context, long sessionId, GameCommand command);
}
