using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;

public interface IGameInputOperation : IOperationPort
{
    ValueTask<GameCommand> ReadCommandAsync(IGameplayContext context);
}
