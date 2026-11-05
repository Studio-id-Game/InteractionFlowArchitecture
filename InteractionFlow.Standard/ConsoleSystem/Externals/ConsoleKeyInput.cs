using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>キーの到着をポーリングし、入力を残さずにキャンセルできる実装です。</summary>
    public sealed class ConsoleKeyInput : ConsoleInputBase, IConsoleKeyInputPort
    {
        private const int PollIntervalMilliseconds = 20;

        /// <inheritdoc/>
        public ValueTask<ConsoleKeyInfo> ReadKeyAsync(IFlowContext context) => ReadAsync(context, ReadKeyCoreAsync);

        private static async ValueTask<ConsoleKeyInfo> ReadKeyCoreAsync(CancellationToken token)
        {
            if (Console.IsInputRedirected)
                throw new NotSupportedException("Key input requires a terminal.");

            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (Console.KeyAvailable)
                {
                    token.ThrowIfCancellationRequested();
                    return Console.ReadKey(intercept: true);
                }

                await Task.Delay(PollIntervalMilliseconds, token).ConfigureAwait(false);
            }
        }
    }
}
