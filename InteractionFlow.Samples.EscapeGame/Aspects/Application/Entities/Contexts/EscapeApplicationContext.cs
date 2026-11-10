using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System.Diagnostics.CodeAnalysis;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;

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
    public IViewAccess View => Access;
    public ILifetimeAccess Lifetime => Access;

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameplayAccess) ? Gameplay
            : typeof(T) == typeof(IViewAccess) ? View
            : typeof(T) == typeof(ILifetimeAccess) ? Lifetime : null;
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
