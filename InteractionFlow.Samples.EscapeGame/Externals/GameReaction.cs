using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameReaction : Reaction, IGameReaction
{
    public override void ForceResetMemoryState() { }

    public async ValueTask<ReactionEnd> BeginAsync(EscapeApplicationContext context)
    {
        // 未公開セッションには Active の待機を要求しません。初期化はここだけで明示します。
        var change = context.World.BeginSession(context.SessionId);
        var failure = GameMutationValidation.Presentation(change, nameof(BeginAsync));
        if (failure is not null || change.Status == MutationStatus.Stopped)
            return GetEnd(failure);

        return await PresentAsync(context, change.Presentation!.Value);
    }

    public async ValueTask<ReactionEnd> EnableSelectionAsync(EscapeApplicationContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Control.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.World.EnterSelecting(context.SessionId);
            if (status == MutationStatus.Deferred)
                continue;

            return GetEnd(GameMutationValidation.RequiredTransition(status, nameof(EnableSelectionAsync)));
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> SelectAsync(EscapeApplicationContext context, GameCommand command)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Control.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.World.ApplySelection(context.SessionId, command);
            if (status == MutationStatus.Deferred)
                continue;

            // 選択の Rejected は通常の無効操作。アクセス管理が理由を表示へ反映済みです。
            return GetEnd(GameMutationValidation.Selection(status, nameof(SelectAsync)));
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> ApplyPlayerAsync(EscapeApplicationContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Control.WaitForActiveAsync(context.SessionId, token))
        {
            // 事前の再開確認直後にもポーズし得るため、更新側の Deferred 検査は残します。
            var change = context.World.ApplyPlayer(context.SessionId);
            if (change.Status == MutationStatus.Deferred)
                continue;

            var failure = GameMutationValidation.Presentation(change, nameof(ApplyPlayerAsync));
            if (failure is not null || change.Status == MutationStatus.Stopped)
                return GetEnd(failure);

            return await PresentAsync(context, change.Presentation!.Value);
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> ApplyEnemyAsync(EscapeApplicationContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Control.WaitForActiveAsync(context.SessionId, token))
        {
            var change = context.World.ApplyEnemy(context.SessionId);
            if (change.Status == MutationStatus.Deferred)
                continue;

            var failure = GameMutationValidation.Presentation(change, nameof(ApplyEnemyAsync));
            if (failure is not null || change.Status == MutationStatus.Stopped)
                return GetEnd(failure);

            return await PresentAsync(context, change.Presentation!.Value);
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> ShowResultAsync(EscapeApplicationContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Control.WaitForActiveAsync(context.SessionId, token))
        {
            var change = context.World.ShowResult(context.SessionId);
            if (change.Status == MutationStatus.Deferred)
                continue;

            var failure = GameMutationValidation.Presentation(change, nameof(ShowResultAsync));
            if (failure is not null || change.Status == MutationStatus.Stopped)
                return GetEnd(failure);

            return await PresentAsync(context, change.Presentation!.Value);
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> EnableResultInputAsync(EscapeApplicationContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Control.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.World.EnableResultInput(context.SessionId);
            if (status == MutationStatus.Deferred)
                continue;

            return GetEnd(GameMutationValidation.RequiredTransition(status, nameof(EnableResultInputAsync)));
        }

        return GetEnd();
    }

    private static async ValueTask<ReactionEnd> PresentAsync(EscapeApplicationContext context, PresentationKey key)
    {
        var token = context.Cancellation.GetToken();
        var completion = await context.Presentation.WaitAsync(key, token);
        if (completion == PresentationCompletion.Stopped)
            return GetEnd();

        // 通知受領後のポーズも、次のゲーム段階へ進む前に待ちます。
        await context.Control.WaitForActiveAsync(context.SessionId, token);
        return GetEnd();
    }
}
