using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Globalization;
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
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

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
    /// Dependency Node ルールの診断メッセージが Resources 経由で英語・日本語に切り替わることを確認します。
    /// </summary>
    [Fact]
    public void DependencyNodeResources_LocalizesMessages()
    {
        var previousCulture = Resources.Culture;

        try
        {
            Resources.Culture = CultureInfo.GetCultureInfo("en");
            Assert.Equal(
                "IDependencyNode class must be sealed or declare 'params IDependencyNode[] dependency'",
                Resources.DependencyNodeMustBeSealedOrHaveParams);

            Resources.Culture = CultureInfo.GetCultureInfo("ja");
            Assert.Equal(
                "IDependencyNode クラスは sealed にするか 'params IDependencyNode[] dependency' を宣言する必要があります",
                Resources.DependencyNodeMustBeSealedOrHaveParams);
        }
        finally
        {
            Resources.Culture = previousCulture;
        }
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
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderType"));

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
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "ThirdParty", "Client"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// SystemFlows 層が ExternalPorts / Externals / Builders 層へ依存したとき、
    /// それぞれアーキテクチャ違反として診断することを確認します。
    /// </summary>
    [Theory]
    [InlineData("ExternalPorts", "ExternalPort")]
    [InlineData("Externals", "ExternalWorker")]
    [InlineData("Builders", "BuilderWorker")]
    public async Task SystemFlows_DisallowedLayerReferences_Report(string targetLayer, string targetType)
    {
        var source = $$"""
            namespace App.{{targetLayer}}
            {
                public class {{targetType}}
                {
                }
            }

            namespace App.SystemFlows
            {
                public class CustomFlow
                {
                    public App.{{targetLayer}}.{{targetType}} Dependency { get; }
                }
            }
            """;

        var expected = ExpectedHidden(12, 22 + targetLayer.Length + targetType.Length, 12, 32 + targetLayer.Length + targetType.Length)
            .WithArguments(ExpectedLayerDependencyDetail("SystemFlows", targetLayer, targetType));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// Interactions 層が SystemFlows / Externals / Builders 層へ依存したとき、
    /// それぞれアーキテクチャ違反として診断することを確認します。
    /// </summary>
    [Theory]
    [InlineData("SystemFlows", "SystemFlow")]
    [InlineData("Externals", "ExternalWorker")]
    [InlineData("Builders", "BuilderWorker")]
    public async Task Interactions_DisallowedLayerReferences_Report(string targetLayer, string targetType)
    {
        var source = $$"""
            namespace App.{{targetLayer}}
            {
                public class {{targetType}}
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.{{targetLayer}}.{{targetType}} Dependency { get; }
                }
            }
            """;

        var expected = ExpectedHidden(12, 22 + targetLayer.Length + targetType.Length, 12, 32 + targetLayer.Length + targetType.Length)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", targetLayer, targetType));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// ExternalPorts 層が Entities 以外の管理対象層へ依存したとき、
    /// 抽象境界から具象フローや実装へ逆向きに依存する違反として診断することを確認します。
    /// </summary>
    [Theory]
    [InlineData("SystemFlows", "SystemFlow")]
    [InlineData("Interactions", "UseCase")]
    [InlineData("Externals", "ExternalWorker")]
    [InlineData("Builders", "BuilderWorker")]
    public async Task ExternalPorts_DisallowedLayerReferences_Report(string targetLayer, string targetType)
    {
        var source = $$"""
            namespace App.{{targetLayer}}
            {
                public class {{targetType}}
                {
                }
            }

            namespace App.ExternalPorts
            {
                public interface IExternalPort
                {
                    App.{{targetLayer}}.{{targetType}} Create();
                }
            }
            """;

        var expected = ExpectedHidden(12, 15 + targetLayer.Length + targetType.Length, 12, 21 + targetLayer.Length + targetType.Length)
            .WithArguments(ExpectedLayerDependencyDetail("ExternalPorts", targetLayer, targetType));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// Entities 層が他の管理対象層や許可されていない外部 namespace へ依存したとき、
    /// 最内層の Domain から外側へ依存する違反として診断することを確認します。
    /// </summary>
    [Theory]
    [InlineData("SystemFlows", "SystemFlow", "SystemFlows")]
    [InlineData("Interactions", "UseCase", "Interactions")]
    [InlineData("ExternalPorts", "ExternalPort", "ExternalPorts")]
    [InlineData("Externals", "ExternalWorker", "Externals")]
    [InlineData("Builders", "BuilderWorker", "Builders")]
    [InlineData("ThirdParty", "Client", "App.ThirdParty")]
    public async Task Entities_DisallowedReferences_Report(string targetNamespace, string targetType, string targetShowName)
    {
        var source = $$"""
            namespace App.{{targetNamespace}}
            {
                public class {{targetType}}
                {
                }
            }

            namespace App.Entities
            {
                public class Entity
                {
                    public App.{{targetNamespace}}.{{targetType}} Dependency { get; }
                }
            }
            """;

        var expected = ExpectedHidden(12, 22 + targetNamespace.Length + targetType.Length, 12, 32 + targetNamespace.Length + targetType.Length)
            .WithArguments(ExpectedLayerDependencyDetail("Entities", targetShowName, targetType));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 各レイヤーが許可された管理対象層へ依存したとき、
    /// レイヤー依存ルール上の正当な参照として診断しないことを確認します。
    /// </summary>
    [Theory]
    [InlineData("SystemFlows", "Interactions", "UseCase")]
    [InlineData("SystemFlows", "Entities", "Entity")]
    [InlineData("Interactions", "ExternalPorts", "ExternalPort")]
    [InlineData("Interactions", "Entities", "Entity")]
    [InlineData("Externals", "ExternalPorts", "ExternalPort")]
    [InlineData("Externals", "Entities", "Entity")]
    [InlineData("ExternalPorts", "Entities", "Entity")]
    [InlineData("Builders", "SystemFlows", "SystemFlow")]
    [InlineData("Builders", "Interactions", "UseCase")]
    [InlineData("Builders", "ExternalPorts", "ExternalPort")]
    [InlineData("Builders", "Externals", "ExternalWorker")]
    [InlineData("Builders", "Entities", "Entity")]
    public async Task AllowedLayerReferences_DoNotReport(string sourceLayer, string targetLayer, string targetType)
    {
        var source = $$"""
            namespace App.{{targetLayer}}
            {
                public class {{targetType}}
                {
                }
            }

            namespace App.{{sourceLayer}}
            {
                public class Source
                {
                    public App.{{targetLayer}}.{{targetType}} Dependency { get; }
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// Builders 層が許可ルートに含まれていない外部 namespace へ依存しても、
    /// 構築責務を持つ層の外部参照として診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task Builders_ExternalDependency_DoNotReport()
    {
        var source = """
            namespace ThirdParty
            {
                public class Client
                {
                }
            }

            namespace App.Builders
            {
                public class Builder
                {
                    public ThirdParty.Client Client { get; }
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// メソッドの戻り値型が禁止された層の型である場合、
    /// メソッド定義から戻り値型への依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task MethodReturnType_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.Builders.BuilderWorker Create()
                    {
                        return null;
                    }
                }
            }
            """;

        var expected = ExpectedHidden(12, 43, 12, 49)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 型パラメータ制約が禁止された層の型である場合、
    /// ジェネリック型定義から制約型への依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task TypeParameterConstraint_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase<T> where T : App.Builders.BuilderWorker
                {
                }
            }
            """;

        var expected = ExpectedHidden(10, 18, 10, 25)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 基底クラスが禁止された層の型である場合、
    /// 継承関係から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task BaseType_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderBase
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase : App.Builders.BuilderBase
                {
                }
            }
            """;

        var expected = ExpectedHidden(10, 18, 10, 25)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderBase"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 実装 interface が禁止された層の型である場合、
    /// interface 実装関係から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task InterfaceType_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public interface IBuilderContract
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase : App.Builders.IBuilderContract
                {
                }
            }
            """;

        var expected = ExpectedHidden(10, 18, 10, 25)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "IBuilderContract"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// フィールド定義の型が禁止された層の型である場合、
    /// フィールド定義から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task FieldType_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    private App.Builders.BuilderWorker dependency;
                }
            }
            """;

        var expected = ExpectedHidden(12, 44, 12, 54)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// メソッド引数の型が禁止された層の型である場合、
    /// メソッド定義から引数型への依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task MethodParameterType_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public void Run(App.Builders.BuilderWorker dependency)
                    {
                    }
                }
            }
            """;

        var expected = ExpectedHidden(12, 21, 12, 24)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 禁止された層の型を new したとき、
    /// オブジェクト生成式から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task ObjectCreation_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public object Create()
                    {
                        return new App.Builders.BuilderWorker();
                    }
                }
            }
            """;

        var expected = ExpectedHidden(14, 20, 14, 52)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 禁止された層の型に定義されたフィールドを参照したとき、
    /// フィールド参照式から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task FieldReference_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public static class BuilderWorker
                {
                    public static readonly string Name = "";
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public string GetName()
                    {
                        return App.Builders.BuilderWorker.Name;
                    }
                }
            }
            """;

        var expected = ExpectedHidden(15, 20, 15, 51)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 禁止された層の型に定義されたプロパティを参照したとき、
    /// プロパティ参照式から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task PropertyReference_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public static class BuilderWorker
                {
                    public static string Name { get; } = "";
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public string GetName()
                    {
                        return App.Builders.BuilderWorker.Name;
                    }
                }
            }
            """;

        var expected = ExpectedHidden(15, 20, 15, 51)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// ローカル変数の宣言型が禁止された層の型である場合、
    /// 変数宣言から依存違反を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task VariableDeclarator_DisallowedLayerReference_Reports()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public void Run()
                    {
                        App.Builders.BuilderWorker dependency = null;
                    }
                }
            }
            """;

        var expected = ExpectedHidden(14, 40, 14, 57)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// interactionflow_enabled が false の場合、
    /// 禁止された層への依存があっても Analyzer が診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task DisabledAnalyzer_DoesNotReport()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.Builders.BuilderWorker Dependency { get; }
                }
            }
            """;

        await VerifyAsync(source, enabled: false);
    }

    /// <summary>
    /// interactionflow_mode に指定した severity が診断に反映され、
    /// Error モードでは Error 診断として報告されることを確認します。
    /// </summary>
    [Fact]
    public async Task ModeError_ReportsErrorSeverity()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.Builders.BuilderWorker Dependency { get; }
                }
            }
            """;

        var expected = new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithSpan(12, 43, 12, 53)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, mode: "Error", expected: expected);
    }

    /// <summary>
    /// interactionflow_mode に不正な値を指定した場合、
    /// 既定の Warning 診断として報告されることを確認します。
    /// </summary>
    [Fact]
    public async Task InvalidMode_FallsBackToWarningSeverity()
    {
        var source = """
            namespace App.Builders
            {
                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.Builders.BuilderWorker Dependency { get; }
                }
            }
            """;

        var expected = new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(12, 43, 12, 53)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, mode: "Banana", expected: expected);
    }

    /// <summary>
    /// 既定の許可ルートである System namespace への依存は、
    /// 管理対象外の外部依存であっても診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task DefaultAllowedRootSystem_DoNotReport()
    {
        var source = """
            namespace App.Interactions
            {
                public class UseCase
                {
                    public System.Text.StringBuilder Builder { get; }
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// interactionflow_allowed_roots に指定した外部 namespace とその子 namespace への依存は、
    /// 許可済み外部依存として診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task AllowedRoots_PrefixMatch_DoNotReport()
    {
        var source = """
            namespace ThirdParty.Lib
            {
                public class Client
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public ThirdParty.Lib.Client Client { get; }
                }
            }
            """;

        await VerifyAsync(source, additionalSources: null, allowedRoots: "ThirdParty");
    }

    /// <summary>
    /// interactionflow_allowed_roots に似た名前を指定していても、
    /// namespace 境界が一致しない外部依存は許可せず診断することを確認します。
    /// </summary>
    [Fact]
    public async Task AllowedRoots_DoesNotMatchPartialNamespace_Reports()
    {
        var source = """
            namespace ThirdPartyX
            {
                public class Client
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public ThirdPartyX.Client Client { get; }
                }
            }
            """;

        var expected = ExpectedHidden(12, 35, 12, 41)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "ThirdPartyX", "Client"));

        await VerifyAsync(source, additionalSources: null, allowedRoots: "ThirdParty", expected: expected);
    }

    /// <summary>
    /// Nullable や配列などの複合型の内側に禁止された層の型が含まれる場合、
    /// 合成型ではなく内包された型への依存違反として診断することを確認します。
    /// </summary>
    [Fact]
    public async Task ComplexTypeShapes_WithDisallowedInnerTypes_Report()
    {
        var source = """
            #nullable enable

            namespace App.Builders
            {
                public struct BuilderValue
                {
                }

                public class BuilderWorker
                {
                }
            }

            namespace App.Interactions
            {
                public class UseCase
                {
                    public App.Builders.BuilderValue? Maybe { get; }

                    public App.Builders.BuilderWorker[] Items { get; }
                }
            }
            """;

        var expectedNullable = ExpectedHidden(18, 43, 18, 48)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderValue"));
        var expectedArray = ExpectedHidden(20, 45, 20, 50)
            .WithArguments(ExpectedLayerDependencyDetail("Interactions", "Builders", "BuilderWorker"));

        await VerifyAsync(source, expectedNullable, expectedArray);
    }

    /// <summary>
    /// 通常コンストラクタで受け取った IDependencyNode 系の引数を Dependency が返す配列へ含めている場合、
    /// Dependency Node ルールが診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_NormalConstructor_IncludesDependencies_DoNotReport()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public interface IPort : IDependencyNode
                {
                }

                public abstract class NodeClass : IDependencyNode
                {
                    private readonly IDependencyNode[] dependency;

                    public NodeClass(IPort node1, IDependencyNode node2, params IDependencyNode[] dependency)
                    {
                        this.dependency = [node1, node2, .. dependency];
                    }

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependency;
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// 通常コンストラクタで IDependencyNode 系の引数を Dependency auto-property に直接代入している場合、
    /// Dependency Node ルールが診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_NormalConstructor_AssignsDependencyProperty_DoNotReport()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public sealed class Test : IDependencyNode
                {
                    public Test(IDependencyNode node1)
                    {
                        Dependency = new IDependencyNode[] { node1 };
                    }

                    public ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// sealed な IDependencyNode 実装クラスの通常コンストラクタに params IDependencyNode[] が無い場合でも、
    /// 継承拡張対象外として診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_SealedConstructor_MissingParams_DoNotReport()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public sealed class NodeClass : IDependencyNode
                {
                    private readonly IDependencyNode[] dependency;

                    public NodeClass(IDependencyNode node1)
                    {
                        dependency = [node1];
                    }

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependency;
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// 通常コンストラクタで受け取った IDependencyNode 系の引数が Dependency に含まれていない場合、
    /// 欠落した引数を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_NormalConstructor_MissingDependency_Reports()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public interface IPort : IDependencyNode
                {
                }

                public abstract class NodeClass : IDependencyNode
                {
                    private readonly IDependencyNode[] dependency;

                    public NodeClass(IPort node1, params IDependencyNode[] dependency)
                    {
                        this.dependency = [.. dependency];
                    }

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependency;
                }
            }
            """;

        var expected = ExpectedDependencyHidden(22, 32, 22, 37);

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// プライマリコンストラクタで受け取った IDependencyNode 系の引数を Dependency が返す配列へ含めている場合、
    /// Dependency Node ルールが診断しないことを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_PrimaryConstructor_IncludesDependencies_DoNotReport()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public interface IPort : IDependencyNode
                {
                }

                public abstract class NodeClass(IPort node1, params IDependencyNode[] dependency) : IDependencyNode
                {
                    private readonly IDependencyNode[] dependencies = [node1, .. dependency];

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependencies;
                }
            }
            """;

        await VerifyAsync(source);
    }

    /// <summary>
    /// IDependencyNode 実装クラスを継承するプライマリコンストラクタで、
    /// 親へ流していない IDependencyNode 系の引数を診断することを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_PrimaryConstructor_MissingBaseForward_Reports()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public interface IPort : IDependencyNode
                {
                }

                public abstract class BaseNode(params IDependencyNode[] dependency) : IDependencyNode
                {
                    private readonly IDependencyNode[] dependencies = dependency;

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependencies;
                }

                public abstract class ChildNode(IPort node1, params IDependencyNode[] dependency) : BaseNode(dependency)
                {
                }
            }
            """;

        var expected = ExpectedDependencyHidden(25, 43, 25, 48);

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// 継承拡張される抽象 IDependencyNode クラスの通常コンストラクタに
    /// params IDependencyNode[] が無い場合、診断することを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_AbstractConstructor_MissingParams_Reports()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public interface IPort : IDependencyNode
                {
                }

                public abstract class NodeClass : IDependencyNode
                {
                    private readonly IDependencyNode[] dependency;

                    public NodeClass(IPort node1)
                    {
                        this.dependency = [node1];
                    }

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependency;
                }
            }
            """;

        var expected = ExpectedDependencyHidden(22, 16, 22, 25);

        await VerifyAsync(source, expected);
    }

    /// <summary>
    /// sealed ではない具象 IDependencyNode 実装クラスの通常コンストラクタに
    /// params IDependencyNode[] が無い場合、診断することを確認します。
    /// </summary>
    [Fact]
    public async Task DependencyNode_NonSealedConstructor_MissingParams_Reports()
    {
        var source = """
            using System;
            using InteractionFlow.Core.Entities.Architectures;

            namespace InteractionFlow.Core.Entities.Architectures
            {
                public interface IDependencyNode
                {
                    ReadOnlyMemory<IDependencyNode> Dependency { get; }
                }
            }

            namespace App.Nodes
            {
                public class NodeClass : IDependencyNode
                {
                    private readonly IDependencyNode[] dependency;

                    public NodeClass(IDependencyNode node1)
                    {
                        this.dependency = [node1];
                    }

                    public ReadOnlyMemory<IDependencyNode> Dependency => dependency;
                }
            }
            """;

        var expected = ExpectedDependencyHidden(18, 16, 18, 25);

        await VerifyAsync(source, expected);
    }

    private static string ExpectedLayerDependencyDetail(string sourceLayer, string targetLayer, string typeName)
        => string.Format(Resources.LayerDependencyDisallowedReference, sourceLayer, targetLayer, typeName);

    private static DiagnosticResult ExpectedHidden(int startLine, int startColumn, int endLine, int endColumn)
        => new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DiagnosticId, DiagnosticSeverity.Hidden)
            .WithSpan(startLine, startColumn, endLine, endColumn);

    private static DiagnosticResult ExpectedDependencyHidden(int startLine, int startColumn, int endLine, int endColumn)
        => new DiagnosticResult(InteractionFlowAnalyzersAnalyzer.DependencyNodeDiagnosticId, DiagnosticSeverity.Hidden)
            .WithSpan(startLine, startColumn, endLine, endColumn);

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
        => VerifyAsync(source, additionalSources: null, expected: expected);

    private static Task VerifyAsync(string source, bool enabled, params DiagnosticResult[] expected)
        => VerifyAsync(source, additionalSources: null, enabled: enabled, expected: expected);

    private static async Task VerifyAsync(
        string source,
        (string fileName, string content)[]? additionalSources = null,
        bool enabled = true,
        string mode = "Hidden",
        string? allowedRoots = null,
        params DiagnosticResult[] expected)
    {
        var diagnosticSeverity = mode switch
        {
            "Error" => "error",
            "Warning" => "warning",
            "Info" => "suggestion",
            "Hidden" => "hidden",
            _ => "warning",
        };
        var allowedRootsOption = allowedRoots == null
            ? ""
            : $"{OptionValues.Keys.interactionflow_allowed_roots} = {allowedRoots}";
        var editorconfig = $"""
            root = true

            [*.cs]

            {OptionValues.Keys.interactionflow_enabled} = {enabled}
            {OptionValues.Keys.interactionflow_mode} = {mode}
            {allowedRootsOption}
            dotnet_diagnostic.{InteractionFlowAnalyzersAnalyzer.DiagnosticId}.severity = {diagnosticSeverity}
            dotnet_diagnostic.{InteractionFlowAnalyzersAnalyzer.DependencyNodeDiagnosticId}.severity = {diagnosticSeverity}

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
