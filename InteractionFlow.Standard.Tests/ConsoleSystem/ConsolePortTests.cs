using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Externals.Operations;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.Builders;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.ReactionPorts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.ConsoleSystem
{
    public sealed class ConsolePortTests
    {
        [Theory]
        [InlineData("Alice", "Hello, Alice!")]
        [InlineData("", "Hello, !")]
        [InlineData(null, "EOF")]
        public async Task InteractionUsesReplacementPorts(string? line, string expected)
        {
            var builder = new ScopeBuilder();
            builder.Apply(ConsoleProfile.Default);
            builder.UseFunction<IConsoleLineInputPort, TestLineInput>();
            builder.UseFunction<IConsoleOutputPort, RecordingOutput>();
            builder.UseInteraction<GreetingInteraction>();

            using var scope = builder.BuildScope();
            var input = Assert.IsType<TestLineInput>(scope.GetService(typeof(IConsoleLineInputPort)));
            var output = Assert.IsType<RecordingOutput>(scope.GetService(typeof(IConsoleOutputPort)));
            var interaction = Assert.IsType<GreetingInteraction>(scope.GetService(typeof(GreetingInteraction)));
            input.Line = line;

            var end = await interaction.ExecuteAsync(new FlowContext());

            Assert.False(end.HasException);
            Assert.Equal(expected, Assert.Single(output.Lines));
            Assert.Equal(1, input.ReadCount);
        }

        // テスト用の External は標準実装の内部構造ではなく、Port の契約だけに依存する。
        private sealed class TestLineInput : Operation, IConsoleLineInputPort
        {
            public string? Line { get; set; }
            public int ReadCount { get; private set; }

            public ValueTask<string?> ReadLineAsync(IFlowContext context)
            {
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                ReadCount++;
                return new(Line);
            }

            public override void ForceResetMemoryState()
            {
                Line = null;
                ReadCount = 0;
            }
        }

        private sealed class RecordingOutput : Reaction, IConsoleOutputPort
        {
            public List<string> Lines { get; } = [];

            public ValueTask<ReactionEnd> WriteAsync(
                IFlowContext context, string text, ConsoleTextStyle style = default)
            {
                Lines.Add(text);
                return new(GetEnd());
            }

            public ValueTask<ReactionEnd> WriteLineAsync(
                IFlowContext context, string text, ConsoleTextStyle style = default)
                => WriteAsync(context, text, style);

            public override void ForceResetMemoryState() => Lines.Clear();
        }

        private sealed class GreetingInteraction(
            IConsoleLineInputPort input,
            IConsoleOutputPort output,
            IExceptionPort<Exception> exceptionPort,
            ICancellationPort cancellationPort)
            : Interaction(exceptionPort, cancellationPort, input, output)
        {
            protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
            {
                var line = await input.ReadLineAsync(context);
                return await output.WriteLineAsync(context, line is null ? "EOF" : $"Hello, {line}!");
            }
        }
    }
}
