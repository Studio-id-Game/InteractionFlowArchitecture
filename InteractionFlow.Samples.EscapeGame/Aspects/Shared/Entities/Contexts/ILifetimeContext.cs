using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Core.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

public interface ILifetimeContext : IFlowContext
{
    ILifetimeAccess Lifetime { get; }
}
