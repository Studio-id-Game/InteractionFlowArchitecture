using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;

// 演出時間はViewだけが所有します。完了通知は描画を提出した後に行います。
public sealed class PresentationClock
{
    private PresentationKey? key;
    private long controlRevision;
    public double ElapsedSeconds { get; private set; }
    public double DurationSeconds { get; private set; }
    public bool IsComplete => key.HasValue && ElapsedSeconds >= DurationSeconds;

    public void Reset()
    {
        key = null;
        controlRevision = 0;
        ElapsedSeconds = DurationSeconds = 0;
    }

    public GameEvent? Update(GameInfo<GameViewInfo> game, double deltaSeconds)
    {
        var state = game.Info;
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (state.Event is not { } e) { Reset(); return null; }
        DurationSeconds = e.Kind switch
        {
            GameEventKind.PlayerMove or GameEventKind.EnemyMove => .15,
            GameEventKind.Switch or GameEventKind.Unlock => .2,
            _ => 0
        };
        if (key != e.Key) { key = e.Key; ElapsedSeconds = 0; }
        else if (state.Mode == ExecutionMode.Active && controlRevision == state.ControlRevision)
            ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
        if (state.Mode == ExecutionMode.Active && state.SkipRequested) ElapsedSeconds = DurationSeconds;
        controlRevision = state.ControlRevision;
        return e;
    }
}
