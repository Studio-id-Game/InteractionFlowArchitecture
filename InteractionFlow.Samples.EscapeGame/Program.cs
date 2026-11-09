using InteractionFlow.Samples.EscapeGame.Builders;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.SystemFlows;
using InteractionFlow.Standard.Builders;
using System;

namespace InteractionFlow.Samples.EscapeGame;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var builder = new ScopeBuilder();
        builder.Apply(new EscapeGameProfile());

        using var scope = builder.BuildScope();
        using var flow = scope.BuildSystemFlow<WindowSystemFlow, IApplicationContext>();
        using var context = new EscapeApplicationContext();

        return flow.ExecuteAsync(context).GetAwaiter().GetResult().HasException ? 1 : 0;
    }
}
