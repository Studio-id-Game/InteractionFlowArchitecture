using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>例外を標準エラーに表示し、未解決の失敗として保持します。</summary>
    public sealed class ConsoleExceptionHandling : ExceptionHandling
    {
        /// <inheritdoc/>
        public override void ForceResetMemoryState() => ThrowException = false;

        /// <inheritdoc/>
        protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
        {
            var failure = ConsoleOutputWriter.WriteErrorLine(exception.ToString());
            return new(GetEnd(ConsoleOutputWriter.Combine(exception, failure)));
        }
    }
}
