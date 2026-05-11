using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public abstract class Interaction(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        params IFlowNode[] dependency)
        : Interaction<Exception>(exceptionPort, cancellationPort, dependency)
    {

    }

    public abstract class Interaction<TException>(
    IExceptionPort<TException> exceptionPort,
    ICancellationPort cancellationPort,
    params IFlowNode[] dependency)
    : IInteraction
    where TException : Exception
    {
        public ReadOnlySpan<IFlowNode> Dependency => (IFlowNode[])[ExceptionPort, CancellationPort, .. dependency];
        protected IExceptionPort<TException> ExceptionPort => exceptionPort;
        protected ICancellationPort CancellationPort => cancellationPort;

        public abstract Task<FlowEndToken> InteractWithUserAsync(IFlowContext context);

        protected async Task<FlowEndToken> HandleCancellationAsync(IFlowContext context, OperationCanceledException e)
        {
            return await CancellationPort.HandleCancellation(context, e);
        }

        protected async Task<FlowEndToken> HandleExceptionAsync(IFlowContext context, TException e)
        {
            return await ExceptionPort.HandleExceptionAsync(context, e);
        }

        /// <summary>
        /// Interaction の実行本体を、ライブラリ標準の例外ハンドリング内で実行します。
        /// <para>
        /// <see cref="OperationCanceledException"/> および <typeparamref name="TException"/> は捕捉され、
        /// <see cref="FlowEndToken"/> に変換されます。
        /// </para>
        /// <para>
        /// <paramref name="attachCancellation"/> が指定された場合、
        /// Task の cancellation 監視を追加します。
        /// この監視は cancellation 同期のみを目的としており、
        /// 例外処理は本メソッドの main await path 側で行われます。
        /// </para>
        /// </summary>
        /// <param name="context">
        /// Interaction の実行コンテキスト。
        /// </param>
        /// <param name="function">
        /// 実行する Interaction 本体。
        /// </param>
        /// <param name="attachCancellation">
        /// 非同期 cancellation 時に実行される追加処理。
        /// </param>
        /// <returns>
        /// Railway 変換後の <see cref="FlowEndToken"/> を返します。
        /// </returns>
        protected async Task<FlowEndToken> TryCatchBlock(IFlowContext context, Func<IFlowContext, Task<FlowEndToken>> function, Func<ValueTask>? attachCancellation = null)
        {
            try
            {
                if (context.TryGetCanceledException(out var e))
                {
                    throw e!;
                }

                var task = function(context);

                if (attachCancellation != null)
                {
                    context.Cancellation.AddCancelableTask(CancelableTask(task, attachCancellation));

                    static async Task CancelableTask(Task task, Func<ValueTask>? attachCancellation)
                    {
                        try
                        {
                            await task;
                        }
                        catch (OperationCanceledException)
                        {
                            if (attachCancellation != null)
                            {
                                await attachCancellation();
                            }
                        }
                        catch (Exception)
                        {
                            // ここでの例外は、下の return await task; でハンドリングする事を前提として、握りつぶす。
                            // これにより、ObserveLifetime は純粋な寿命監視の目的を果たす。 
                        }
                    }
                }

                return await task;
            }
            catch (OperationCanceledException e)
            {
                e = new OperationCanceledException($"{this.GetName()} Interaction was canceled.", e);
                var end = await HandleCancellationAsync(context, e);
                end.Exception = e;
                return end;
            }
            catch (TException e)
            {
                var end = await HandleExceptionAsync(context, e);
                end.Exception = e;
                return end;
            }
        }

    }
}
