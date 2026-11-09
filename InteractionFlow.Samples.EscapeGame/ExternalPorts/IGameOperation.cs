using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IGameOperation : IOperationPort
{
    ValueTask<GameCommand> ReadAsync(EscapeApplicationContext context);
    bool Submit(WindowContext context, long sessionId, GameCommand command);
}
