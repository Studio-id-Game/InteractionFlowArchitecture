using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Interactions;

public sealed class ShowResult(
    IGameReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<EscapeApplicationContext>(exceptionPort, cancellationPort, reaction)
{
    protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
    {
        var shown = await reaction.ShowResultAsync(context);
        if (shown.HasException)
            return shown;

        // 結果画面はポーズ対象外。提示後に受付を開くまでを一つの目的にまとめます。
        // 停止との競合は受付Reaction側で再確認します。
        return await reaction.EnableResultInputAsync(context);
    }
}
