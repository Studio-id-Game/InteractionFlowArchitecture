using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Builders;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.SystemFlows;
using InteractionFlow.Standard.Builders;
using System;

namespace InteractionFlow.Samples.Raylib;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var builder = new ScopeBuilder();
        builder.Apply(new RaylibCounterProfile());

        using var scope = builder.BuildScope();
        using var flow = scope.BuildSystemFlow<WindowSystemFlow, CounterFlowContext>();

        using var context = new CounterFlowContext();

        Console.WriteLine(DependencyTreeView.GetDependencyTreeText(flow.Root));

        var end = flow.ExecuteAsync(context).GetAwaiter().GetResult();

        return end.HasException ? 1 : 0;
    }
}
