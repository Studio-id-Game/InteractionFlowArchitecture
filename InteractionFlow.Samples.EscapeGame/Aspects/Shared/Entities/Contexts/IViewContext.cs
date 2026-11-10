using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

public interface IViewContext : ILifetimeContext
{
    IViewAccess View { get; }
}
