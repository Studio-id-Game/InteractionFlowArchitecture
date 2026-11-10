using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

public interface IGameplayContext : ILifetimeContext
{
    long SessionId { get; }
    IGameplayAccess Gameplay { get; }
}
