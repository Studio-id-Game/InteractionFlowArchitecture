using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Externals;
using InteractionFlow.Standard.Builders;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Builders;

public sealed class ApplicationProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<IGameInputOperation, GameInputOperation>();
    }
}
