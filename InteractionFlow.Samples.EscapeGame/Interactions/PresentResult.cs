using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Interactions;

public sealed class PresentResult(
    IGameplayReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, reaction)
{
    protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var shown = await reaction.PresentResultAsync(context);
        if (shown.HasException)
            return shown;

        // 結果画面はポーズ対象外。提示後に受付を開くまでを一つの目的にまとめます。
        // 停止との競合は受付Reaction側で再確認します。
        return await reaction.EnableResultInputAsync(context);
    }
}
