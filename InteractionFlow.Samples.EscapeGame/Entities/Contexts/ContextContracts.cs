using InteractionFlow.Core.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

public interface ILifetimeContext : IFlowContext
{
    IApplicationLifetimeAccess Lifetime { get; }
}

public interface IGameplayContext : ILifetimeContext
{
    long SessionId { get; }
    IGameplayAccess Gameplay { get; }
}

public interface IGameViewContext : ILifetimeContext
{
    IGameViewAccess View { get; }
}

// 両実行を構成するSystemFlowだけが受け取る合成契約です。
public interface IApplicationContext : IGameplayContext, IGameViewContext { }
