using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.FileSystem.Externals;
using InteractionFlow.Standard.SerializationSystem.Externals;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.FileSystem
{
    public sealed class FilePersistenceTests
    {
        private static readonly string[] ExpectedConcurrentValues = ["first complete value", "second complete value"];

        [Theory]
        [InlineData(".bin")]
        [InlineData(".data.json")]
        [InlineData("")]
        public async Task SaveUpdateLoadAndEnumerateRoundTrip(string extension)
        {
            using var directory = new FileSystemTestDirectory();
            var serializer = new ProbeSerializer();
            var store = new Store(() => directory.Root, serializer, extension);
            Assert.False(Directory.Exists(directory.Root));
            const string id = "group/日本語.v2";

            Success(await store.Save(id, "first"));
            Success(await store.Save(id, "second"));
            Assert.Equal("second", Value(await store.Load(id, new IOException("no previous value"))));
            Assert.Equal(id, Assert.Single(Value(await store.GetAllIds())));
            Success(await store.Exists(id));
            Success(await store.Delete(id));
            Success(await store.Delete(id));
            Assert.False((await store.Exists(id)).Try(out _));
            Assert.False(serializer.LastStream!.CanRead);
            Assert.False(serializer.LastStream.CanWrite);
        }

        [Fact]
        public async Task FailedInputDoesNotResolveRootOrCallSerializer()
        {
            using var directory = new FileSystemTestDirectory();
            var serializer = new ProbeSerializer();
            var store = new Store(() => throw new InvalidOperationException("must not resolve"), serializer);
            var result = await store.Save("note", new IOException("upstream failed"));

            Assert.False(result.Try(out var error));
            Assert.Contains("upstream failed", error!.ToString());
            Assert.Equal(0, store.RootCalls);
            Assert.Equal(0, serializer.SerializeCalls);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Theory]
        [InlineData("failure", false)]
        [InlineData("throw", false)]
        [InlineData("partial", false)]
        [InlineData("closed", false)]
        [InlineData("failure", true)]
        [InlineData("throw", true)]
        [InlineData("partial", true)]
        [InlineData("closed", true)]
        public async Task SerializationFailurePreservesTargetAndCleansTemporaryFile(string mode, bool newFile)
        {
            using var directory = new FileSystemTestDirectory();
            var serializer = new ProbeSerializer();
            var store = new Store(() => directory.Root, serializer);
            Directory.CreateDirectory(directory.Root);
            var target = directory.PathFor("note.bin");
            var original = Encoding.UTF8.GetBytes("original data");
            if (!newFile) await File.WriteAllBytesAsync(target, original, TestContext.Current.CancellationToken);

            serializer.Write = async (_, stream) =>
            {
                if (mode is "partial" or "closed")
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("partial"), TestContext.Current.CancellationToken);
                if (mode == "throw") throw new IOException("serializer threw");
                if (mode == "closed")
                {
                    await stream.DisposeAsync();
                    return stream;
                }
                return new IOException("serialization failed");
            };

            if (mode == "throw") await Assert.ThrowsAsync<IOException>(() => store.Save("note", "replacement"));
            else Assert.False((await store.Save("note", "replacement")).Try(out _));
            if (newFile) Assert.False(File.Exists(target));
            else Assert.Equal(original, await File.ReadAllBytesAsync(target, TestContext.Current.CancellationToken));
            Assert.Equal(newFile ? 0 : 1, Directory.EnumerateFiles(directory.Root).Count());
        }

        [Fact]
        public async Task CleanupFailureRetainsSerializationFailure()
        {
            using var directory = new FileSystemTestDirectory();
            var serializer = new ProbeSerializer
            {
                Write = async (_, stream) =>
                {
                    var path = ((FileStream)stream).Name;
                    await stream.DisposeAsync();
                    File.Delete(path);
                    Directory.CreateDirectory(path);
                    return new IOException("serialization failed before cleanup");
                }
            };
            var store = new Store(() => directory.Root, serializer);
            var result = await store.Save("note", "value");
            Assert.False(result.Try(out var failure));
            Assert.IsType<AggregateException>(failure!.InnerException);
            Assert.Contains("serialization failed before cleanup", failure.ToString());
            Assert.False(File.Exists(directory.PathFor("note.bin")));
        }

        [Fact]
        public async Task FailedCommitPreservesExistingDirectoryAndRemovesTemporaryFile()
        {
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.PathFor("note.bin"));
            var store = new Store(() => directory.Root, new ProbeSerializer());

            Assert.False((await store.Save("note", "value")).Try(out _));
            Assert.True(Directory.Exists(directory.PathFor("note.bin")));
            Assert.Empty(Directory.EnumerateFiles(directory.Root));
        }

        [Fact]
        public async Task LockedTargetIsNotDeletedDuringFailedReplacement()
        {
            Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows file-sharing semantics are required.");
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root, new ProbeSerializer());
            Success(await store.Save("note", "original"));
            var target = directory.PathFor("note.bin");
            using (var held = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.False((await store.Save("note", "replacement")).Try(out _));
            }
            Assert.Equal("original", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
            Assert.Single(Directory.EnumerateFiles(directory.Root));
        }

        [Fact]
        public async Task ConcurrentSavesPublishOnlyCompleteValues()
        {
            using var directory = new FileSystemTestDirectory();
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var staged = 0;
            var serializer = new ProbeSerializer
            {
                Write = async (input, stream) =>
                {
                    Assert.True(input.Try(out var value, out _));
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(value), TestContext.Current.CancellationToken);
                    if (Interlocked.Increment(ref staged) == 2) ready.TrySetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                    return stream;
                }
            };
            var store = new Store(() => directory.Root, serializer);
            var first = store.Save("note", "first complete value");
            var second = store.Save("note", "second complete value");
            try
            {
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            }
            finally
            {
                release.TrySetResult();
            }

            var results = await Task.WhenAll(first, second);
            Assert.Contains(results, result => result.Try(out _));
            var saved = File.ReadAllText(directory.PathFor("note.bin"));
            Assert.Contains(saved, ExpectedConcurrentValues);
            Assert.Single(Directory.EnumerateFiles(directory.Root));
        }

        [Fact]
        public async Task ReadOnlyTargetReportsFailureInsteadOfDeleteSuccess()
        {
            Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows read-only file semantics are required.");
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root, new ProbeSerializer());
            Success(await store.Save("note", "original"));
            var target = directory.PathFor("note.bin");
            File.SetAttributes(target, FileAttributes.ReadOnly);
            try
            {
                Success(await store.Exists("note"));
                Assert.False((await store.Delete("note")).Try(out var failure));
                Assert.Contains("UnauthorizedAccessException", failure!.ToString());
                Assert.Equal("original", File.ReadAllText(target));
            }
            finally
            {
                File.SetAttributes(target, FileAttributes.Normal);
            }
        }

        [Fact]
        public async Task ReplacingDifferentlyCasedTargetStillEnumeratesSavedId()
        {
            Assert.SkipUnless(OperatingSystem.IsWindows(), "Case-insensitive Windows file paths are required.");
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.Root);
            File.WriteAllText(directory.PathFor("note.DATA.JSON"), "original");
            var store = new Store(() => directory.Root, new ProbeSerializer(), ".data.json");

            Success(await store.Save("note", "updated"));
            Assert.Equal("updated", Value(await store.Load("note", new IOException())));
            Assert.Equal("note", Assert.Single(Value(await store.GetAllIds())));
        }

        [Fact]
        public async Task MissingRootOperationsDoNotCreateAnything()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root, new ProbeSerializer());

            Assert.Empty(Value(await store.GetAllIds()));
            Assert.False((await store.Load("note", new IOException("no old value"))).Try(out _, out _));
            Assert.False((await store.Exists("note")).Try(out _));
            Success(await store.Delete("note"));
            Assert.Equal(4, store.RootCalls);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Fact]
        public async Task EachOperationResolvesRootOnceAndLaterOperationsCanSwitchRoot()
        {
            using var directory = new FileSystemTestDirectory();
            var selectedRoot = directory.PathFor("first");
            var store = new Store(() => selectedRoot, new ProbeSerializer());

            Success(await store.Save("note", "one"));
            Assert.Equal(1, store.RootCalls);
            selectedRoot = directory.PathFor("second");
            Success(await store.Save("note", "two"));
            Assert.Equal(2, store.RootCalls);
            Assert.Equal("two", Value(await store.Load("note", new IOException())));
            Assert.Equal(3, store.RootCalls);
            Success(await store.Exists("note"));
            Assert.Equal(4, store.RootCalls);
            Assert.Single(Value(await store.GetAllIds()));
            Assert.Equal(5, store.RootCalls);
            Success(await store.Delete("note"));
            Assert.Equal(6, store.RootCalls);
            Assert.Equal("one", File.ReadAllText(directory.PathFor("first", "note.bin")));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RelativeRootRemainsStableThroughCommitAndCleanupWhenCurrentDirectoryChanges(bool fail)
        {
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.PathFor("first"));
            Directory.CreateDirectory(directory.PathFor("second"));
            var originalDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = directory.PathFor("first");
                Directory.CreateDirectory(directory.PathFor("first", "data"));
                File.WriteAllText(directory.PathFor("first", "data", "note.bin"), "original");
                var serializer = new ProbeSerializer
                {
                    Write = async (_, stream) =>
                    {
                        Environment.CurrentDirectory = directory.PathFor("second");
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("value"), TestContext.Current.CancellationToken);
                        if (fail) return new IOException("failed after changing current directory");
                        return stream;
                    }
                };
                var store = new Store(() => "data", serializer);
                var result = await store.Save("note", "value");
                Assert.Equal(!fail, result.Try(out _));
                Assert.Equal(1, store.RootCalls);
                Assert.Equal(fail ? "original" : "value",
                    File.ReadAllText(directory.PathFor("first", "data", "note.bin")));
                Assert.Single(Directory.EnumerateFiles(directory.PathFor("first", "data")));
                Assert.False(Directory.Exists(directory.PathFor("second", "data")));
                Assert.Empty(Value(await store.GetAllIds()));
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
            }
        }

        [Theory]
        [InlineData("../escape")]
        [InlineData(".")]
        [InlineData("")]
        [InlineData(".interactionflow-save-reserved")]
        public async Task InvalidTargetDoesNotCreateFiles(string id)
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.PathFor("root"), new ProbeSerializer(), "");
            Assert.False((await store.Save(id, "value")).Try(out _));
            Assert.False(Directory.Exists(directory.Root));
        }

        [Fact]
        public async Task AbsoluteTargetsAndNeighborRootsAreRejected()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.PathFor("root"), new ProbeSerializer());
            Assert.False((await store.Save(directory.PathFor("absolute"), "value")).Try(out _));
            Assert.False((await store.Save("../root-neighbor/note", "value")).Try(out _));
            Assert.False(Directory.Exists(directory.Root));
        }

        [Theory]
        [InlineData(".data.json")]
        [InlineData("")]
        public async Task EnumerationExcludesTemporaryFilesAndUsesExactSuffix(string extension)
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root, new ProbeSerializer(), extension);
            Success(await store.Save("note.v2", "value"));
            File.WriteAllText(directory.PathFor(".interactionflow-save-stale"), "unfinished");
            if (extension.Length != 0) File.WriteAllText(directory.PathFor("external.DATA.JSON"), "external");

            Assert.Equal("note.v2", Assert.Single(Value(await store.GetAllIds())));
            Success(await store.Delete("note.v2"));
            Assert.True(File.Exists(directory.PathFor(".interactionflow-save-stale")));
        }

        [Fact]
        public async Task ParseAndRootFailuresAreNotReplacedByEmptyResults()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root, new ProbeSerializer());
            Success(await store.Save("note", "value"));
            store.Parse = _ => throw new FormatException("invalid ID");
            Assert.False((await store.GetAllIds()).Try(out _, out var parseFailure));
            Assert.Contains("invalid ID", parseFailure!.ToString());

            var failing = new Store(() => throw new IOException("root unavailable"), new ProbeSerializer());
            Assert.False((await failing.GetAllIds()).Try(out _, out var rootFailure));
            Assert.Contains("root unavailable", rootFailure!.ToString());
        }

        [Fact]
        public async Task WrongTargetTypesAreFailures()
        {
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.PathFor("note.bin"));
            var store = new Store(() => directory.Root, new ProbeSerializer());
            Assert.False((await store.Exists("note")).Try(out _));
            Assert.False((await store.Delete("note")).Try(out _));
            var fileRoot = directory.PathFor("file-root");
            File.WriteAllText(fileRoot, "not a directory");
            Assert.False((await new Store(() => fileRoot, new ProbeSerializer()).GetAllIds()).Try(out _, out _));
        }

        [Theory]
        [InlineData("txt")]
        [InlineData(".")]
        [InlineData(".data/json")]
        [InlineData(".data\\json")]
        [InlineData(".*")]
        [InlineData(null)]
        public void InvalidExtensionIsRejectedAtConstruction(string? extension)
        {
            Assert.ThrowsAny<ArgumentException>(() => new Store(() => ".", new ProbeSerializer(), extension!));
        }

        [Fact]
        public async Task LoadPropagatesSerializerExceptionAfterClosingStream()
        {
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.Root);
            await File.WriteAllTextAsync(directory.PathFor("note.bin"), "value", TestContext.Current.CancellationToken);
            var failure = new InvalidOperationException("deserialize");
            var serializer = new ProbeSerializer { ReadException = failure };
            var store = new Store(() => directory.Root, serializer);
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => store.Load("note", "")));
            Assert.False(serializer.LastStream!.CanRead);
        }

        [Fact]
        public async Task ThrownSerializerAndCleanupFailuresAreBothPropagated()
        {
            using var directory = new FileSystemTestDirectory();
            var failure = new InvalidOperationException("serializer threw before cleanup");
            var serializer = new ProbeSerializer
            {
                Write = async (_, stream) =>
                {
                    var path = ((FileStream)stream).Name;
                    await stream.DisposeAsync();
                    File.Delete(path);
                    Directory.CreateDirectory(path);
                    throw failure;
                }
            };
            var store = new Store(() => directory.Root, serializer);
            var aggregate = await Assert.ThrowsAsync<AggregateException>(() => store.Save("note", "value"));
            Assert.Same(failure, aggregate.InnerExceptions[0]);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
            Assert.False(File.Exists(directory.PathFor("note.bin")));
        }

        private static void Success(Result result) => Assert.True(result.Try(out var failure), failure?.ToString());

        private static T Value<T>(Result<T> result)
        {
            Assert.True(result.Try(out var value, out var failure), failure?.ToString());
            return value!;
        }

        private sealed class Store(Func<string> root, ProbeSerializer serializer, string extension = ".bin")
            : FilePersistence<string, string>(serializer, extension)
        {
            public int RootCalls { get; private set; }
            public Func<string, string> Parse { get; set; } = path => path.Replace('\\', '/');

            public override string GetCurrentRootPath()
            {
                RootCalls++;
                return root();
            }

            protected override string GetRelativePathWithoutExtension(string id) => id;
            protected override string GetIdFromRelativePathWithoutExtension(string relativePath) => Parse(relativePath);
        }

        private sealed class ProbeSerializer : StreamSerializer<string>
        {
            public Func<Result<string>, Stream, Task<Result<Stream>>>? Write { get; set; }
            public Exception? ReadException { get; init; }
            public int SerializeCalls { get; private set; }
            public Stream? LastStream { get; private set; }

            public override async Task<Result<Stream>> Serialize(Result<string> inputValue, Result<Stream> refData)
            {
                SerializeCalls++;
                if (!refData.Try(out var stream, out var streamFailure)) return streamFailure;
                LastStream = stream;
                if (Write != null) return await Write(inputValue, stream);
                if (!inputValue.Try(out var value, out var valueFailure)) return valueFailure;
                await stream.WriteAsync(Encoding.UTF8.GetBytes(value), TestContext.Current.CancellationToken);
                return stream;
            }

            public override async Task<Result<string>> Deserialize(Result<Stream> inputData, Result<string> refValue)
            {
                if (!inputData.Try(out var stream, out var failure)) return failure;
                LastStream = stream;
                if (ReadException != null) throw ReadException;
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
            }
        }
    }
}
