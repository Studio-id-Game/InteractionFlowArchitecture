using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Externals;

public sealed class GameplayReaction : Reaction, IGameplayReaction
{
    public override void ForceResetMemoryState() { }

    public ReactionEnd ApplyControlRequest(IGameplayContext context)
    {
        if (!context.Gameplay.TryReadControlInput(out var input)) return GetEnd();
        var change = context.Gameplay.UpdateGameState(input.SessionId,
            state => WorldRules.ApplyControl(state, input), allowWhilePaused: true);
        return GetEnd(GameUpdateValidation.Selection(change.Status, nameof(ApplyControlRequest)));
    }

    public async ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context)
    {
        // 未公開セッションには Active の待機を要求しません。初期化はここだけで明示します。
        var change = context.Gameplay.BeginSession(context.SessionId, WorldRules.BeginSession);
        var failure = GameUpdateValidation.Presentation(change, nameof(BeginSessionAsync));
        if (failure is not null || change.Status == GameUpdateStatus.Stopped)
            return GetEnd(failure);

        return await WaitForPresentationAndResumeAsync(context, change.Presentation!.Value);
    }

    public async ValueTask<ReactionEnd> BeginActionSelectionAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.BeginActionSelection).Status;
            if (status == GameUpdateStatus.Deferred)
                continue;

            return GetEnd(GameUpdateValidation.RequiredTransition(status, nameof(BeginActionSelectionAsync)));
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> HandleCommandAsync(IGameplayContext context, GameCommand command)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.Gameplay.UpdateGameState(context.SessionId, state => WorldRules.ApplyInputCommand(state, command)).Status;
            if (status == GameUpdateStatus.Deferred)
                continue;

            // 選択のRejectedは通常の無効操作。ゲーム状態の理由をViewが表示へ変換します。
            return GetEnd(GameUpdateValidation.Selection(status, nameof(HandleCommandAsync)));
        }

        return GetEnd();
    }

    public async ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            // 事前の再開確認直後にもポーズし得るため、更新側の Deferred 検査は残します。
            var change = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.ApplyPlayer);
            if (change.Status == GameUpdateStatus.Deferred)
                continue;

            var failure = GameUpdateValidation.Presentation(change, nameof(ApplyPlayerActionAsync));
            if (failure is not null || change.Status == GameUpdateStatus.Stopped)
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
            var change = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.ApplyEnemy);
            if (change.Status == GameUpdateStatus.Deferred)
                continue;

            var failure = GameUpdateValidation.Presentation(change, nameof(ApplyEnemyActionAsync));
            if (failure is not null || change.Status == GameUpdateStatus.Stopped)
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
            var change = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.BeginResultPresentation);
            if (change.Status == GameUpdateStatus.Deferred)
                continue;

            var failure = GameUpdateValidation.Presentation(change, nameof(PresentResultAsync));
            if (failure is not null || change.Status == GameUpdateStatus.Stopped)
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
            var status = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.EnableResultInput).Status;
            if (status == GameUpdateStatus.Deferred)
                continue;

            return GetEnd(GameUpdateValidation.RequiredTransition(status, nameof(EnableResultInputAsync)));
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
