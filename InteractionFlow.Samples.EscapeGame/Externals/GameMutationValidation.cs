using InteractionFlow.Samples.EscapeGame.Entities;
using System;

namespace InteractionFlow.Samples.EscapeGame.Externals;

// フローが前提とする更新結果だけを検証します。状態更新・待機・通知は担当しません。
internal static class GameMutationValidation
{
    internal static Exception? Selection(MutationStatus status, string operation)
        => status is MutationStatus.Applied or MutationStatus.Rejected or MutationStatus.Stopped
            ? null : Unexpected(status, operation);

    internal static Exception? RequiredTransition(MutationStatus status, string operation)
        => status is MutationStatus.Applied or MutationStatus.Stopped
            ? null : Unexpected(status, operation);

    internal static Exception? Presentation(MutationResult change, string operation)
    {
        var failure = RequiredTransition(change.Status, operation);
        if (failure is not null)
            return failure;

        if (change.Status == MutationStatus.Applied && change.Presentation is null)
            return new InvalidOperationException($"{operation}: an applied update requires a presentation key.");

        return null;
    }

    private static Exception Unexpected(MutationStatus status, string operation)
        => new InvalidOperationException($"{operation}: update returned {status}; the flow precondition was not met.");
}
