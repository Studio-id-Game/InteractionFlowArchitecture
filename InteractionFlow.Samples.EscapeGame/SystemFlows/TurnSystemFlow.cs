using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class TurnSystemFlow(
    EnableSelection enable,
    SelectAction select,
    ApplyPlayerAction player,
    ApplyEnemyAction enemy)
    : SystemFlow<EscapeApplicationContext>(enable, select, player, enemy)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
    {
        var end = await enable.ExecuteAsync(context);
        if (!await CanContinueAsync(context, end))
            return end;

        while (!context.World.GetProgress(context.SessionId).HasConfirmedAction)
        {
            end = await select.ExecuteAsync(context);
            if (!await CanContinueAsync(context, end))
                return end;
        }

        end = await player.ExecuteAsync(context);
        if (!await CanContinueAsync(context, end)
            || context.World.GetProgress(context.SessionId).Outcome != GameOutcome.Running)
        {
            return end;
        }

        return await enemy.ExecuteAsync(context);
    }

    private static async ValueTask<bool> CanContinueAsync(EscapeApplicationContext context, InteractionEnd end)
    {
        if (end.HasException || context.World.GetProgress(context.SessionId).Stopping)
            return false;

        try
        {
            return await context.Control.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken())
                && !context.World.GetProgress(context.SessionId).Stopping;
        }
        catch (System.OperationCanceledException) when (context.World.ShutdownRequested)
        {
            return false;
        }
    }
}
