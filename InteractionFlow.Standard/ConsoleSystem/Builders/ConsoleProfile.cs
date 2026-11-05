using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Externals.Operations;
using InteractionFlow.Standard.ConsoleSystem.Externals.Reactions;
using InteractionFlow.Standard.ConsoleSystem.Externals.SilentExternals;
using System;

namespace InteractionFlow.Standard.ConsoleSystem.Builders
{
    /// <summary>Console の標準実装を登録します。端末操作やイベント購読は開始しません。</summary>
    public sealed class ConsoleProfile : IScopeProfile
    {
        /// <summary>状態を持たない共有登録プロファイル。</summary>
        public static IScopeProfile Default { get; } = new ConsoleProfile();
        /// <inheritdoc/>
        public void Configure(IScopeServices builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            builder.UseFunction<IConsoleLineInputPort, ConsoleLineInput>();
            builder.UseFunction<IConsoleKeyInputPort, ConsoleKeyInput>();
            builder.UseFunction<IConsoleOutputPort, ConsoleOutput>();
            builder.UseFunction<IConsoleScreenPort, ConsoleScreen>();
            builder.UseFunction<IConsoleEnvironmentPort, ConsoleEnvironment>();
            builder.UseFunction<IConsoleCancellationBindingPort, ConsoleCancellationBinding>();
            builder.UseFunction<IExceptionPort<Exception>, ConsoleExceptionHandling>();
            builder.UseFunction<ICancellationPort, ConsoleCancellationHandling>();
        }
    }
}
