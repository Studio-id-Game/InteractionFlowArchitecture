using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using System;

namespace InteractionFlow.Core.Interactions
{
    /// <summary>
    /// 任意の IFlowContext で実行できる Interaction の実装基底クラスです。
    /// </summary>
    /// <param name="exceptionPort">通常の例外を処理するポート。</param>
    /// <param name="cancellationPort">キャンセルを処理するポート。</param>
    /// <param name="dependency">この Interaction が依存するフローノード。</param>
    public abstract class Interaction(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        params IDependencyNode[] dependency)
        : Interaction<IFlowContext>(exceptionPort, cancellationPort, dependency), IInteraction
    {
    }
}
