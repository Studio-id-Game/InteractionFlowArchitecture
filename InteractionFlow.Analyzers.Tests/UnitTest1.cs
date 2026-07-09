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
    /// <summary>
    /// Interactions 層のコードが void 戻り値の Builders 層メソッドを呼び出したとき、
    /// 呼び出し式から依存先の型を検出し、Builders への依存違反として診断することを確認します。
    /// </summary>
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

    /// <summary>
    /// interactionflow_allowed_roots に大文字小文字の異なる重複値が含まれていても、
    /// 許可ルートを大文字小文字非依存で正規化し、許可済み外部 namespace への依存を診断しないことを確認します。
    /// </summary>
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

    /// <summary>
    /// 同じ違反型がジェネリック型引数内に複数回現れても、
    /// 1 つの解析対象内では同じ型を重複診断せず、1 件だけ診断することを確認します。
    /// </summary>
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

    /// <summary>
    /// Nullable、配列、タプル、ローカル関数、ラムダ、匿名型を含む複雑な型形状で Entities 層に依存しても、
    /// 許可された Entities 依存として扱い、合成型を global namespace 依存として誤診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task AllowedEntityDependencies_WithComplexTypeShapes_DoNotReport()
    {
        var source = """
            #nullable enable

            using System;
            using System.Collections.Generic;

            namespace App.Entities
            {
                public class Entity
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.Entities.Entity? Maybe { get; }

                    public App.Entities.Entity[] Items { get; }

                    public (App.Entities.Entity Entity, List<string> Names) Create()
                    {
                        App.Entities.Entity Local() => new();
                        Func<App.Entities.Entity> factory = () => new App.Entities.Entity();
                        var tuple = (Entity: Local(), Other: factory());
                        var anon = new { Value = tuple.Entity };

                        return (anon.Value, new List<string>());
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// ソース側 namespace が Interaction Flow の管理対象層に属していない場合、
    /// そのコードが Builders 層の型を参照してもアーキテクチャ違反として診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task OutsideLayerSource_DoesNotReport()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Utilities
            {
                public class Helper
                {
                    public App.Builders.BuilderWorker Create()
                    {
                        return new App.Builders.BuilderWorker();
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// Interactions 層のコードが許可ルートに含まれていない外部 namespace の型に依存したとき、
    /// 外部依存違反として診断し、表示名と対象型名が期待どおりになることを確認します。
    /// </summary>
    [Fact]
    public async Task ExternalDependency_NotInAllowedRoots_Reports()
    {
        var source = """
            namespace ThirdParty
            {
                public class Client
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public ThirdParty.Client Client { get; }
                }
            }
            """;

        var expected = new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DiagnosticId, DiagnosticSeverity.Hidden)
            .WithSpan(12, 34, 12, 40)
            .WithArguments("Interactions", "ThirdParty", "Client");

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
            dotnet_diagnostic.{InteractionFlowAnalyzersAnalyzer.DiagnosticId}.severity = hidden

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
