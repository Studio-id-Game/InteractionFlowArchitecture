using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// 実行・公開は Reaction の責務です。この型はアクセス値とスコープを提供します。
public sealed class EscapeApplicationContext : FlowContext
{
    private EscapeAccess? access;
    public EscapeApplicationContext()
    {
        access = new();
    }

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
    private EscapeApplicationContext(EscapeAccess shared, long sessionId)
    {
        access = shared;
        SessionId = sessionId;
    }
    private EscapeAccess Access => access ?? throw new ObjectDisposedException(nameof(EscapeApplicationContext));
    public long SessionId { get; }
    public IGameInputAccess Input => Access;
    public IGameWorldAccess World => Access;
    public IGameDisplayAccess Display => Access;
    public IGamePresentationAccess Presentation => Access;
    public IGameControlAccess Control => Access;

    // 初期値のセッションを割り当てるだけです。公開・提示開始は BeginSession で行います。
    public EscapeApplicationContext CreateSessionContext()
    {
        var shared = Access;
        return new EscapeApplicationContext(this, shared.CreateSession());
    }
    public EscapeApplicationContext CreateIndependentContext() => new(Access, SessionId);

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameInputAccess) ? Input
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
        if (value is IGameInputAccess or IGameWorldAccess or IGameDisplayAccess
            or IGamePresentationAccess or IGameControlAccess || IsEntry(value!))
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
