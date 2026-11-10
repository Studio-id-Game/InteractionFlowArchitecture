using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System;
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
        return GetEnd(ValidateUpdate(change.Status, nameof(ApplyControlRequest), allowRejected: true));
    }

    public async ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context)
    {
        // 未公開セッションには Active の待機を要求しません。初期化はここだけで明示します。
        var change = context.Gameplay.BeginSession(context.SessionId, WorldRules.BeginSession);
        var failure = ValidatePresentation(change, nameof(BeginSessionAsync));
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

            return GetEnd(ValidateUpdate(status, nameof(BeginActionSelectionAsync)));
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
            return GetEnd(ValidateUpdate(status, nameof(HandleCommandAsync), allowRejected: true));
        }

        return GetEnd();
    }

    public ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context)
        => UpdateAndPresentAsync(context, WorldRules.ApplyPlayer, nameof(ApplyPlayerActionAsync));

    public ValueTask<ReactionEnd> ApplyEnemyActionAsync(IGameplayContext context)
        => UpdateAndPresentAsync(context, WorldRules.ApplyEnemy, nameof(ApplyEnemyActionAsync));

    public ValueTask<ReactionEnd> PresentResultAsync(IGameplayContext context)
        => UpdateAndPresentAsync(context, WorldRules.BeginResultPresentation, nameof(PresentResultAsync));

    public async ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            var status = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.EnableResultInput).Status;
            if (status == GameUpdateStatus.Deferred)
                continue;

            return GetEnd(ValidateUpdate(status, nameof(EnableResultInputAsync)));
        }

        return GetEnd();
    }

    // フローの前提だけを検証し、状態更新・待機・通知は行いません。
    internal static Exception? ValidateUpdate(GameUpdateStatus status, string operation, bool allowRejected = false)
        => status is GameUpdateStatus.Applied or GameUpdateStatus.Stopped
            || allowRejected && status == GameUpdateStatus.Rejected
            ? null
            : new InvalidOperationException($"{operation}: update returned {status}; the flow precondition was not met.");

    internal static Exception? ValidatePresentation(GameUpdateResult change, string operation)
    {
        var failure = ValidateUpdate(change.Status, operation);
        if (failure is not null)
            return failure;

        if (change.Status == GameUpdateStatus.Applied && change.Presentation is null)
            return new InvalidOperationException($"{operation}: an applied update requires a presentation key.");

        return null;
    }

    private static async ValueTask<ReactionEnd> UpdateAndPresentAsync(
        IGameplayContext context, Func<GameState, GameStateChange> update, string operation)
    {
        var token = context.Cancellation.GetToken();
        while (await context.Gameplay.WaitForActiveAsync(context.SessionId, token))
        {
            // 再開確認直後にもポーズし得るため、更新側のDeferred検査と再試行は残します。
            var change = context.Gameplay.UpdateGameState(context.SessionId, update);
            if (change.Status == GameUpdateStatus.Deferred)
                continue;

            var failure = ValidatePresentation(change, operation);
            if (failure is not null || change.Status == GameUpdateStatus.Stopped)
                return GetEnd(failure);

            return await WaitForPresentationAndResumeAsync(context, change.Presentation!.Value);
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
