using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>色を出力単位で適用・復元して標準出力に書き込みます。</summary>
    public sealed class ConsoleOutput : Reaction, IConsoleOutputPort
    {
        /// <summary>端末の状態は変更しません。</summary>
        public override void ForceResetMemoryState()
        {
        }

        /// <inheritdoc/>
        public ValueTask<ReactionEnd> WriteAsync(
            IFlowContext context, string text, ConsoleTextStyle style = default)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            if (context is null) throw new ArgumentNullException(nameof(context));

            try
            {
                ConsoleOutputWriter.Write(text, style);
                return new(GetEnd());
            }
            catch (Exception e)
            {
                return new(GetEnd(e));
            }
        }

        /// <inheritdoc/>
        public ValueTask<ReactionEnd> WriteLineAsync(
            IFlowContext context, string text, ConsoleTextStyle style = default)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            if (context is null) throw new ArgumentNullException(nameof(context));

            try
            {
                ConsoleOutputWriter.WriteLine(text, style);
                return new(GetEnd());
            }
            catch (Exception e)
            {
                return new(GetEnd(e));
            }
        }
    }
}
