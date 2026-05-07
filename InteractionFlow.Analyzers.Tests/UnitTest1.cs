using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
namespace InteractionFlow.Analyzers.Tests;

public class InteractionFlowAnalyzersAnalyzerTests
{
    [Fact]
    public async Task Invocation_WithVoidReturn_ReportsContainingTypeDependency()
    {
        var useCaseSource = """
            namespace App.Interactions
            {
                public class UseCase
                {
                    public void Run()
                    {
                        App.Builders.BuilderWorker.Execute();
                    }
                }
            }
            """;

        var workerSource = """
            namespace App.Builders
            {
                public static class BuilderWorker
                {
                    public static void Execute()
                    {
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DiagnosticId, DiagnosticSeverity.Hidden)
            .WithSpan(7, 13, 7, 49)
            .WithArguments("Interactions", "Builders", "BuilderWorker");

        await VerifyAsync(useCaseSource, additionalSources: [("BuilderWorker.cs", workerSource)], expected: expected);
    }

    [Fact]
    public void AllowedRoots_MatchesCaseInsensitive_DoesNotReport()
    {
        var options = new InMemoryAnalyzerConfigOptions(new Dictionary<string, string>
        {
            { OptionValues.Keys.interactionflow_allowed_roots, "thirdparty, ThirdParty,  " }
        });

        var roots = OptionValues.GetAllowedRoots(options);

        Assert.Contains("thirdparty", roots, StringComparer.OrdinalIgnoreCase);
        Assert.Single(roots, x => x.Equals("thirdparty", StringComparison.OrdinalIgnoreCase));
        Assert.False(LayerNames.IsDisallowReference(roots, "App.Interactions", "ThirdParty.Lib", out _, out _));
    }

    [Fact]
    public async Task DuplicateTypeArguments_ReportOnlyOnce()
    {
        var source = """
            using System.Collections.Generic;

            namespace App.Builders
            {
                public class BuilderType
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public Dictionary<App.Builders.BuilderType, App.Builders.BuilderType> Map { get; }
                }
            }
            """;

        var expected = new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DiagnosticId, DiagnosticSeverity.Hidden)
            .WithSpan(14, 79, 14, 82)
            .WithArguments("Interactions", "Builders", "BuilderType");

        await VerifyAsync(source, expected);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
        => VerifyAsync(source, additionalSources: null, expected: expected);

    private static async Task VerifyAsync(string source, (string fileName, string content)[]? additionalSources = null, params DiagnosticResult[] expected)
    {
        var editorconfig = $"""
            root = true

            [*.cs]

            {OptionValues.Keys.interactionflow_enabled} = True
            {OptionValues.Keys.interactionflow_mode} = Hidden

            """;

        var test = new CSharpAnalyzerTest<InteractionFlowAnalyzersAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", editorconfig)
                }
            }
        };

        if (additionalSources != null)
        {
            foreach (var (fileName, content) in additionalSources)
            {
                test.TestState.Sources.Add((fileName, content));
            }
        }

        test.ExpectedDiagnostics.AddRange(expected);


        await test.RunAsync();
    }

    private sealed class InMemoryAnalyzerConfigOptions(IDictionary<string, string> data) : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (data.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
