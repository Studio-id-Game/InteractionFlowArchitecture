using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameplayReaction : Reaction, IGameplayReaction
{
    public override void ForceResetMemoryState() { }

    public async ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context)
    {
        // 未公開セッションには Active の待機を要求しません。初期化はここだけで明示します。
        var change = context.Gameplay.BeginSession(context.SessionId);
        var failure = GameMutationValidation.Presentation(change, nameof(BeginSessionAsync));
        if (failure is not null || change.Status == MutationStatus.Stopped)
            return GetEnd(failure);

        return await WaitForPresentationAndResumeAsync(context, change.Presentation!.Value);
    }

    public async ValueTask<ReactionEnd> BeginActionSelectionAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.Gameplay.BeginActionSelection(context.SessionId);
            if (status == MutationStatus.Deferred)
                continue;

            return GetEnd(GameMutationValidation.RequiredTransition(status, nameof(BeginActionSelectionAsync)));
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> HandleCommandAsync(IGameplayContext context, GameCommand command)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.Gameplay.ApplyInputCommand(context.SessionId, command);
            if (status == MutationStatus.Deferred)
                continue;

            // 選択の Rejected は通常の無効操作。アクセス管理が理由を表示へ反映済みです。
            return GetEnd(GameMutationValidation.Selection(status, nameof(HandleCommandAsync)));
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            // 事前の再開確認直後にもポーズし得るため、更新側の Deferred 検査は残します。
            var change = context.Gameplay.ApplyPlayerAction(context.SessionId);
            if (change.Status == MutationStatus.Deferred)
                continue;

            var failure = GameMutationValidation.Presentation(change, nameof(ApplyPlayerActionAsync));
            if (failure is not null || change.Status == MutationStatus.Stopped)
                return GetEnd(failure);

            return await WaitForPresentationAndResumeAsync(context, change.Presentation!.Value);
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> ApplyEnemyActionAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var change = context.Gameplay.ApplyEnemyAction(context.SessionId);
            if (change.Status == MutationStatus.Deferred)
                continue;

            var failure = GameMutationValidation.Presentation(change, nameof(ApplyEnemyActionAsync));
            if (failure is not null || change.Status == MutationStatus.Stopped)
                return GetEnd(failure);

            return await WaitForPresentationAndResumeAsync(context, change.Presentation!.Value);
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> PresentResultAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var change = context.Gameplay.BeginResultPresentation(context.SessionId);
            if (change.Status == MutationStatus.Deferred)
                continue;

            var failure = GameMutationValidation.Presentation(change, nameof(PresentResultAsync));
            if (failure is not null || change.Status == MutationStatus.Stopped)
                return GetEnd(failure);

            return await WaitForPresentationAndResumeAsync(context, change.Presentation!.Value);
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.Gameplay.EnableResultInput(context.SessionId);
            if (status == MutationStatus.Deferred)
                continue;

            return GetEnd(GameMutationValidation.RequiredTransition(status, nameof(EnableResultInputAsync)));
        }

        return GetEnd();
    }

    private static async ValueTask<ReactionEnd> WaitForPresentationAndResumeAsync(
        IGameplayContext context, PresentationKey key)
    {
        var token = context.Cancellation.GetToken();
        var completion = await context.Gameplay.WaitForPresentationCompletionAsync(key, token);
        if (completion == PresentationCompletion.Stopped)
            return GetEnd();

        // 通知受領後のポーズも、次のゲーム段階へ進む前に待ちます。
        await context.Gameplay.WaitForActiveAsync(context.SessionId, token);
        return GetEnd();
    }
}
