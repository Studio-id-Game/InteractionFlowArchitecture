using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// 入力配送・描画・制御・停止用。ゲームの行動更新や入力待機は提供しません。
public sealed class WindowContext : FlowContext
{
    private EscapeAccess? access;

    public WindowContext()
    {
        access = new();
    }

    // 同型の子は親の追加値とCancellationを共有します。
    public WindowContext(WindowContext parent) : base(parent)
    {
        access = parent.Access;
    }

    private WindowContext(EscapeAccess shared)
    {
        access = shared;
    }

    private EscapeAccess Access => access ?? throw new ObjectDisposedException(nameof(WindowContext));
    public IGameInputWriter Input => Access;
    public IGameSessionAccess Session => Access;
    public IGameDisplayAccess Display => Access;
    public IGamePresentationAccess Presentation => Access;
    public IGameControlAccess Control => Access;

    // ゲーム側へはCancellationだけを引き継ぎ、WindowState/FrameState/Finishingは探索しません。
    public EscapeApplicationContext CreateGameContext()
        => new(Access, EscapeApplicationContext.ApplicationSessionId, this);

    // 描画・停止用の独立ルート。親探索とCancellation共有を行いません。
    public WindowContext CreateIndependentContext() => new(Access);

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameInputWriter) ? Input
            : typeof(T) == typeof(IGameSessionAccess) ? Session
            : typeof(T) == typeof(IGameDisplayAccess) ? Display
            : typeof(T) == typeof(IGamePresentationAccess) ? Presentation
            : typeof(T) == typeof(IGameControlAccess) ? Control : null;
        if (provided is T found)
        {
            value = found;
            return true;
        }

        return base.TryGetCore(out value);
    }

    protected override void ValidateAdditionalValue<T>(T value)
        => EscapeContextValues.ValidateAdditional(value!);

    // Context自身の参照だけを解放し、共有状態や別Contextの実行を停止しません。
    protected override void DisposeCore() => access = null;
}
