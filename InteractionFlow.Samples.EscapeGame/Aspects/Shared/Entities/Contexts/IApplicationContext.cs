
namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

// 両実行を構成するSystemFlowだけが受け取る合成契約です。
public interface IApplicationContext : IGameplayContext, IViewContext { }
