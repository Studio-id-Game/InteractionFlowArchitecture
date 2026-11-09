using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// ゲーム実行用。実行・公開は Reaction の責務で、この型はアクセス値とスコープを提供します。
public sealed class EscapeApplicationContext : EscapeContext
{
    // 0 は特定セッションを指さないゲーム全体のスコープです。実セッションIDは1から始まります。
    public const long ApplicationSessionId = 0;

    private EscapeAccess? access;
    public EscapeApplicationContext()
    {
        access = new();
        SessionId = ApplicationSessionId;
    }

    // 同じ対象セッション・アクセス・Cancellationを共有し、追加値を親へ探索する子スコープです。
    public EscapeApplicationContext(EscapeApplicationContext parent) : base(parent)
    {
        access = parent.Access;
        SessionId = parent.SessionId;
    }
    private EscapeApplicationContext(EscapeApplicationContext parent, long sessionId) : base(parent)
    {
        access = parent.Access;
        SessionId = sessionId;
    }
    internal EscapeApplicationContext(EscapeAccess shared, long sessionId)
    {
        access = shared;
        SessionId = sessionId;
    }
    internal EscapeApplicationContext(EscapeAccess shared, long sessionId, CancellationObject cancellation)
        : base(cancellation)
    {
        access = shared;
        SessionId = sessionId;
    }
    private EscapeAccess Access => access ?? throw new ObjectDisposedException(nameof(EscapeApplicationContext));
    // 固定された対象ID。アクティブセッションへの自動的な追従は行いません。
    public long SessionId { get; }
    public IGameInputReader Input => Access;
    public IGameSessionAccess Session => Access;
    public IGameWorldAccess World => Access;
    public IGamePresentationAccess Presentation => Access;
    public IGameControlAccess Control => Access;

    // ゲーム実行側から呼びます。新規セッションと親のCancellationを共有する子スコープを作ります。
    // 初期値の割当てだけを行い、公開・提示開始はSession.BeginSessionを呼ぶReactionが担当します。
    public EscapeApplicationContext CreateSessionContext()
    {
        var shared = Access;
        return new EscapeApplicationContext(this, shared.CreateSession());
    }
    // 対象IDとアクセスだけを共有する独立ルートです。Cancellationと追加値は共有しません。
    public EscapeApplicationContext CreateIndependentContext() => new(Access, SessionId);

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameInputReader) ? Input
            : typeof(T) == typeof(IGameSessionAccess) ? Session
            : typeof(T) == typeof(IGameWorldAccess) ? World
            : typeof(T) == typeof(IGamePresentationAccess) ? Presentation
            : typeof(T) == typeof(IGameControlAccess) ? Control : null;
        if (provided is T found)
        {
            value = found;
            return true;
        }

        return base.TryGetCore(out value);
    }
    protected override void DisposeCore() => access = null;
}
