using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities.Contexts;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface IWindowReaction : IReactionPort
{
    // すべて呼び出しスレッドで同期完了。描画は EndDrawing まで待ちます。
    ReactionEnd Open(CounterFlowContext context);

    ReactionEnd Draw(CounterFlowContext context);

    // 先行する処理結果に、終了と通知の失敗も含めて返します。
    ReactionEnd Close(CounterFlowContext context, Result result);
}
