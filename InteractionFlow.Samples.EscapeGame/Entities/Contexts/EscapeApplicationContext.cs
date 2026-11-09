using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// 共有アクセスと実行スコープを提供します。進行・実行管理はSystemFlow、公開はReactionが担当します。
public sealed class EscapeApplicationContext : FlowContext
{
    // 0 は特定セッションを指さないアプリのスコープです。実セッションIDは1から始まります。
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
    private EscapeApplicationContext(EscapeApplicationContext parent, ContextInheritance inheritance)
        : base(parent, inheritance)
    {
        access = parent.Access;
        SessionId = parent.SessionId;
    }
    private EscapeAccess Access => access ?? throw new ObjectDisposedException(nameof(EscapeApplicationContext));
    // 固定された対象ID。アクティブセッションへの自動的な追従は行いません。
    public long SessionId { get; }
    public IGameInputReader Input => Access;
    public IGameInputWriter InputWriter => Access;
    public IGameSessionAccess Session => Access;
    public IGameWorldAccess World => Access;
    public IGameDisplayAccess Display => Access;
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
    public EscapeApplicationContext CreateIndependentContext() => new(this, ContextInheritance.None);

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameInputReader) ? Input
            : typeof(T) == typeof(IGameInputWriter) ? InputWriter
            : typeof(T) == typeof(IGameSessionAccess) ? Session
            : typeof(T) == typeof(IGameWorldAccess) ? World
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
    {
        if (value is IGameInputReader or IGameInputWriter or IGameSessionAccess or IGameWorldAccess
            or IGameDisplayAccess or IGamePresentationAccess or IGameControlAccess || IsEntry(value!))
            throw new ArgumentException("Fixed game access contracts and Entry values cannot be added.", nameof(value));
    }

    private static bool IsEntry(object value)
    {
        for (var type = value.GetType(); type is not null; type = type.BaseType)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Entry<>))
                return true;
        return false;
    }

    protected override void DisposeCore() => access = null;
}
