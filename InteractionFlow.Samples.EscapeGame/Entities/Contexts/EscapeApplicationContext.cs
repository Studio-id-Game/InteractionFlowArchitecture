using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// 共有アクセスを作る全体構成用の入口です。実行先には役割別Contextを構築して渡します。
public sealed class EscapeApplicationContext : FlowContext, IApplicationContext
{
    // 0 は特定セッションを指さないアプリのスコープです。実セッションIDは1から始まります。
    public const long NoSessionId = 0;

    private EscapeAccess? access;
    public EscapeApplicationContext()
    {
        access = new();
        SessionId = NoSessionId;
    }

    private EscapeAccess Access => access ?? throw new ObjectDisposedException(nameof(EscapeApplicationContext));
    // 固定された対象ID。アクティブセッションへの自動的な追従は行いません。
    public long SessionId { get; }
    public IGameplayAccess Gameplay => Access;
    public IGameViewAccess View => Access;
    public IApplicationLifetimeAccess Lifetime => Access;

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameplayAccess) ? Gameplay
            : typeof(T) == typeof(IGameViewAccess) ? View
            : typeof(T) == typeof(IApplicationLifetimeAccess) ? Lifetime : null;
        if (provided is T found)
        {
            value = found;
            return true;
        }

        return base.TryGetCore(out value);
    }

    protected override void ValidateAdditionalValue<T>(T value)
        => ContextAccessValidation.Validate(value!);

    protected override void DisposeCore() => access = null;
}
