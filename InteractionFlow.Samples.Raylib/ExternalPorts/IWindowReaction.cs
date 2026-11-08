using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface IWindowReaction : IReactionPort
{
    // すべて呼び出しスレッドで同期完了。描画は EndDrawing まで待ちます。
    ReactionEnd Open(IFlowContext context);

    ReactionEnd Draw(IFlowContext context);

    // 先行する処理結果に、終了と通知の失敗も含めて返します。
    ReactionEnd Close(IFlowContext context, Result result);
}
