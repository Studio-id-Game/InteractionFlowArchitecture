using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Standard.Externals.Operations;
using InteractionFlow.Standard.Externals.Reactions;
using InteractionFlow.Standard.Externals.SilentExternals;
using System;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// コンソール操作に必要な標準ポート実装を登録するスコーププロファイルです。
    /// </summary>
    public class ConsoleBuilder : IScopeProfile
    {
        private readonly bool useCancellation;

        /// <summary>
        /// コンソール入出力と例外・キャンセル表示に必要な標準登録を取得します。
        /// </summary>
        public static IScopeProfile Profile { get; } = new ConsoleBuilder(false);

        /// <summary>
        /// <see cref="Profile"/> に加えて Ctrl+C キャンセル連携を登録する標準登録を取得します。
        /// </summary>
        public static IScopeProfile ProfileUseCancellation { get; } = new ConsoleBuilder(true);

        /// <summary>
        /// Ctrl+C キャンセル連携を登録するかどうかを指定してプロファイルを作成します。
        /// </summary>
        /// <param name="useCancellation">キャンセル連携を登録する場合は <see langword="true"/>。</param>
        protected ConsoleBuilder(bool useCancellation)
        {
            this.useCancellation = useCancellation;
        }

        /// <summary>
        /// コンソール関連の Operation、Reaction、SilentExternal 実装を登録します。
        /// </summary>
        /// <param name="builder">登録先のサービス構成。</param>
        public void Configure(IScopeServices builder)
        {
            builder.UseFunction<IConsoleOperation, ConsoleOperation>();
            builder.UseTransient<IConsoleOperation.IDummy, ConsoleOperation.Dummy>();
            builder.UseFunction<IConsoleWriter, ConsoleWriter>();
            builder.UseFunction<IExceptionPort<Exception>, ConsoleExceptionHandling>();
            builder.UseFunction<ICancellationPort, ConsoleCancellationHandling>();
            builder.UseFunction<IConsoleColorAccess, ConsoleColorAccess>();
            builder.UseFunction<IConsoleCursorPositionAccess, ConsoleCursorPositionAccess>();

            if (useCancellation)
            {
                builder.UseFunction<ICancellationWithConsole, CancellationWithConsole>();
            }
        }
    }
}
