using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using InteractionFlow.ResultAPIGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace InteractionFlow.Analyzers.Tests;

public class ResultApiGeneratorTests
{
    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
        .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToImmutableArray();

    [Fact]
    public void TemplateTargets_GenerateCallableOverloads()
    {
        var compilation = CreateCompilation().AddSyntaxTrees(Parse("""
            using InteractionFlow.Core.ResultAPI;
            using System.Threading.Tasks;

            internal static class Caller
            {
                public static async Task Check()
                {
                    var source = Result<int>.CreateSuccess(1);
                    Result<int> same = source.Then(value => Result<int>.CreateSuccess(value));
                    Result<string> different = source.Then(value => Result<string>.CreateSuccess(value.ToString()));
                    var typed = Result<int, int>.CreateSuccess(2);
                    Result<int, int> typedSame = typed.Then(value => Result<int, int>.CreateSuccess(value));
                    Result<int, int> recovered = typed.ThenError((error, detail) => Result<int, int>.CreateSuccess(detail));

                    var chained = await same.ThenAsync(value => Task.FromResult(Result<int>.CreateSuccess(value)));
                    int resolved = await Task.FromResult(chained).ResolveAsync(
                        value => Task.FromResult(value), error => Task.FromResult(-1));
                    await new ValueTask<Result<int>>(same).IfSuccessValueAsync(async value => await Task.Yield());
                }
            }
            """));

        var (result, updated) = Run(compilation);
        Assert.Empty(Problems(result.Diagnostics));
        Assert.Empty(Problems(updated.GetDiagnostics(TestContext.Current.CancellationToken)));
        Assert.Equal(3, Assert.Single(result.Results).GeneratedSources.Length);
        Assert.Equal(26, CountMethods(updated, "ResultExtensions"));
        Assert.Equal(78, CountMethods(updated, "ResultExtensionsAsync"));
        Assert.Equal(78, CountMethods(updated, "ResultExtensionsValueAsync"));
    }

    [Theory]
    [InlineData("Result<T>", "", true)]
    [InlineData("int", "", false)]
    [InlineData("Result<T>", "where T : class", false)]
    public void HandwrittenOverload_IsRetainedOrReported(string returnType, string constraint, bool compatible)
    {
        var compilation = CreateCompilation().AddSyntaxTrees(Parse($$"""
            namespace InteractionFlow.Core.ResultAPI;
            public static partial class ResultExtensions
            {
                public static {{returnType}} IfSuccess<T>(this Result<T> self, System.Action<T> onSuccess)
                    {{constraint}}
                    => default;
            }
            """));

        var (result, updated) = Run(compilation);
        var diagnostics = Problems(result.Diagnostics).ToArray();
        if (compatible)
            Assert.Empty(diagnostics);
        else
            Assert.Equal("IFRESULT001", Assert.Single(diagnostics).Id);

        Assert.Empty(Problems(updated.GetDiagnostics(TestContext.Current.CancellationToken)));
        Assert.Equal(26, CountMethods(updated, "ResultExtensions"));
    }

    [Fact]
    public void HandwrittenProperty_ReportsConflictAndSkipsOverloadsWithSameName()
    {
        var compilation = CreateCompilation().AddSyntaxTrees(Parse("""
            namespace InteractionFlow.Core.ResultAPI;
            public static partial class ResultExtensions
            {
                public static int IfSuccess => 1;
            }
            """));
        var (result, updated) = Run(compilation);

        Assert.NotEmpty(Problems(result.Diagnostics));
        Assert.All(Problems(result.Diagnostics), diagnostic => Assert.Equal("IFRESULT001", diagnostic.Id));
        Assert.Empty(Problems(updated.GetDiagnostics(TestContext.Current.CancellationToken)));
        Assert.Equal(23, CountMethods(updated, "ResultExtensions"));
    }

    [Fact]
    public void CallbackArguments_UseIndexedOrderThenPropertyName()
    {
        var compilation = CreateCompilation().AddSyntaxTrees(Parse("""
            using InteractionFlow.Core.ResultAPI.Attributes;

            namespace InteractionFlow.Core.ResultAPI;

            public readonly struct MultipleArguments
            {
                public bool IsSuccess => false;
                [ResultAPICallbackArgument(onSuccess: false)] public byte Zulu => 1;
                [ResultAPICallbackArgument(onSuccess: false, index: int.MaxValue)] public long LastIndexed => 2;
                [ResultAPICallbackArgument(onSuccess: false, index: 0)] public bool IndexedZ => true;
                [ResultAPICallbackArgument(onSuccess: false)] public int Alpha => 3;
                [ResultAPICallbackArgument(onSuccess: false, index: 0)] public string IndexedA => "first";
            }

            [ResultAPITarget(typeof(MultipleArguments), null)]
            public static partial class ResultExtensions
            {
                public static void Check(MultipleArguments self)
                {
                    self.IfSuccess(() => { });
                    self.IfError((string indexedA, bool indexedZ, long lastIndexed, int alpha, byte zulu) => { });
                }
            }
            """));
        var (result, updated) = Run(compilation);

        Assert.Empty(Problems(result.Diagnostics));
        Assert.Empty(Problems(updated.GetDiagnostics(TestContext.Current.CancellationToken)));
        Assert.Contains(Assert.Single(result.Results).GeneratedSources, source =>
            source.SourceText.ToString().Contains("onError(self.IndexedA, self.IndexedZ, self.LastIndexed, self.Alpha, self.Zulu)"));
    }

    [Fact]
    public void IncrementalGeneration_ReusesUnchangedAnalysisAndUpdatesEditedDestination()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var compilation = CreateCompilation();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ResultApiGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation, cancellationToken);

        compilation = compilation.AddSyntaxTrees(Parse("internal class Unrelated;"));
        driver = driver.RunGenerators(compilation, cancellationToken);
        var unchanged = GenerationReasons(driver);
        Assert.Equal(3, unchanged.Length);
        Assert.All(unchanged, reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));

        compilation = compilation.AddSyntaxTrees(Parse("""
            namespace InteractionFlow.Core.ResultAPI;
            public static partial class ResultExtensions
            {
                public static Result<T> IfSuccess<T>(this Result<T> self, System.Action<T> onSuccess) => self;
            }
            """));
        driver = driver.RunGenerators(compilation, cancellationToken);
        Assert.Equal(1, GenerationReasons(driver).Count(reason => reason == IncrementalStepRunReason.Modified));

        static IncrementalStepRunReason[] GenerationReasons(GeneratorDriver driver) =>
            Assert.Single(driver.GetRunResult().Results).TrackedSteps["ResultApiGeneration"]
                .SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();
    }

    private static CSharpCompilation CreateCompilation()
    {
        var assembly = typeof(ResultApiGeneratorTests).Assembly;
        var trees = assembly.GetManifestResourceNames()
            .Where(static name => name.StartsWith("ResultApiSources.", StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .Select(name =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
                return Parse(reader.ReadToEnd(), name);
            });
        return CSharpCompilation.Create("ResultApiFixture", trees, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static (GeneratorDriverRunResult Result, Compilation Updated) Run(Compilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ResultApiGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var updated, out _, TestContext.Current.CancellationToken);
        return (driver.GetRunResult(), updated);
    }

    private static SyntaxTree Parse(string source, string path = "") =>
        CSharpSyntaxTree.ParseText(source, path: path);

    private static int CountMethods(Compilation compilation, string name) =>
        compilation.GetTypeByMetadataName("InteractionFlow.Core.ResultAPI." + name)!
            .GetMembers().OfType<IMethodSymbol>().Count(static method => method.MethodKind == MethodKind.Ordinary);

    private static System.Collections.Generic.IEnumerable<Diagnostic> Problems(ImmutableArray<Diagnostic> diagnostics) =>
        diagnostics.Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
}
