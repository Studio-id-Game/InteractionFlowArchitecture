using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameReaction : Reaction, IGameReaction
{
    public override void ForceResetMemoryState() { }

    public ValueTask<ReactionEnd> BeginAsync(EscapeApplicationContext context)
        => ApplyAndPresentAsync(context, () => context.World.BeginSession(context.SessionId), waitBeforeApply: false);

    public ValueTask<ReactionEnd> EnableSelectionAsync(EscapeApplicationContext context)
        => ApplyAsync(context, () => context.World.EnterSelecting(context.SessionId));

    public ValueTask<ReactionEnd> SelectAsync(EscapeApplicationContext context, GameCommand command)
        => ApplyAsync(context, () => context.World.ApplySelection(context.SessionId, command));

    public ValueTask<ReactionEnd> ApplyPlayerAsync(EscapeApplicationContext context)
        => ApplyAndPresentAsync(context, () => context.World.ApplyPlayer(context.SessionId));

    public ValueTask<ReactionEnd> ApplyEnemyAsync(EscapeApplicationContext context)
        => ApplyAndPresentAsync(context, () => context.World.ApplyEnemy(context.SessionId));

    public ValueTask<ReactionEnd> ShowResultAsync(EscapeApplicationContext context)
        => ApplyAndPresentAsync(context, () => context.World.ShowResult(context.SessionId));

    public ValueTask<ReactionEnd> EnableResultInputAsync(EscapeApplicationContext context)
        => ApplyAsync(context, () => context.World.EnableResultInput(context.SessionId));

    private static async ValueTask<ReactionEnd> ApplyAsync(
        EscapeApplicationContext context,
        Func<MutationStatus> apply)
    {
        while (await context.Control.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken()))
        {
            var status = apply();
            if (status == MutationStatus.Deferred)
                continue;

            // Rejected は無効操作のフィードバックであり、実行障害ではありません。
            return GetEnd();
        }

        return GetEnd();
    }

    private static async ValueTask<ReactionEnd> ApplyAndPresentAsync(
        EscapeApplicationContext context,
        Func<MutationResult> apply,
        bool waitBeforeApply = true)
    {
        var token = context.Cancellation.GetToken();

        while (true)
        {
            // Begin は未公開セッションを公開するため、公開前の Current/Active 判定を要求しません。
            if (waitBeforeApply && !await context.Control.WaitForActiveAsync(context.SessionId, token))
                return GetEnd();

            waitBeforeApply = true;
            // ポーズ確認と更新はアクセス管理でも一括検証します。Deferred は同じ仕事を保持します。
            var change = apply();
            if (change.Status == MutationStatus.Deferred)
                continue;

            if (change.Status != MutationStatus.Applied || change.Presentation is not PresentationKey key)
                return GetEnd();

            var completion = await context.Presentation.WaitAsync(key, token);
            if (completion == PresentationCompletion.Stopped)
                return GetEnd();

            // 完了受領後のポーズでも、次のゲーム段階へは進めません。
            await context.Control.WaitForActiveAsync(context.SessionId, token);
            return GetEnd();
        }

    }
}
