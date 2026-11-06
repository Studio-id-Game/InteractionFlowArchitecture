using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.FileSystem.Externals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.FileSystem
{
    public sealed class DirectoryPersistenceTests
    {
        [Fact]
        public async Task SaveAlwaysEnsuresAndSupportsNestedDirectories()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root);
            Assert.False(Directory.Exists(directory.Root));

            Success(await store.Save("group/user", "first"));
            Success(await store.Save("group/user", "second"));
            Assert.Equal(2, store.EnsureCalls);
            Assert.Equal(2, store.SaveCalls);
            Assert.Equal("second", Value(await store.Load("group/user", new IOException("no previous value"))));
            Assert.Equal(1, store.LoadCalls);
            Assert.Equal(2, store.EnsureCalls);
            Assert.Equal(new[] { "ensure", "save", "ensure", "save", "load" }, store.Events);
        }

        [Fact]
        public async Task FailedInputHasNoSideEffectsOrHookCalls()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => throw new IOException("must not resolve"));
            var result = await store.Save("note", new InvalidOperationException("upstream"));

            Assert.False(result.Try(out var failure));
            Assert.Contains("upstream", failure!.ToString());
            Assert.Equal(0, store.RootCalls);
            Assert.Empty(store.Events);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PreparationFailurePreventsSave(bool throwException)
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root)
            {
                EnsureFailure = new IOException("preparation failed"),
                ThrowFromEnsure = throwException
            };

            if (throwException)
                Assert.Same(store.EnsureFailure, await Assert.ThrowsAsync<IOException>(() => store.Save("note", "value")));
            else
            {
                var result = await store.Save("note", "value");
                Assert.False(result.Try(out var failure));
                Assert.Contains("preparation failed", failure!.ToString());
            }
            Assert.Equal(1, store.EnsureCalls);
            Assert.Equal(0, store.SaveCalls);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Fact]
        public async Task EnsureCanPrepareInitialFilesOnEverySave()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root)
            {
                AfterEnsure = path => File.WriteAllText(Path.Combine(path, "initial.txt"), "prepared")
            };
            Success(await store.Save("note", "first"));
            File.Delete(directory.PathFor("note", "initial.txt"));
            Success(await store.Save("note", "second"));

            Assert.Equal("prepared", File.ReadAllText(directory.PathFor("note", "initial.txt")));
            Assert.Equal(2, store.EnsureCalls);
        }

        [Fact]
        public async Task FailedSaveDoesNotPretendToRollBackDerivedChanges()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root) { FailAfterWrite = true };

            Assert.False((await store.Save("note", "partially updated")).Try(out _));
            Assert.Equal("partially updated", File.ReadAllText(directory.PathFor("note", "value.txt")));
        }

        [Fact]
        public async Task MissingRootOperationsDoNotEnsureOrEnumerate()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root);

            Assert.Empty(Value(await store.GetAllIds()));
            Assert.False((await store.Load("note", new IOException())).Try(out _, out _));
            Assert.False((await store.Exists("note")).Try(out _));
            Success(await store.Delete("note"));
            Assert.Equal(4, store.RootCalls);
            Assert.Equal(0, store.EnsureCalls);
            Assert.Equal(0, store.LoadCalls);
            Assert.Equal(0, store.ListCalls);
            Assert.Equal(1, store.DeleteCalls);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MissingTargetStillUsesDeleteOverrideAndPreservesItsFailure(bool throwException)
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root)
            {
                DeleteFailure = new DirectoryNotFoundException("custom cleanup failed"),
                ThrowFromDelete = throwException
            };

            if (throwException)
                Assert.Same(store.DeleteFailure, await Assert.ThrowsAsync<DirectoryNotFoundException>(() => store.Delete("missing")));
            else
            {
                Assert.False((await store.Delete("missing")).Try(out var failure));
                Assert.Contains("custom cleanup failed", failure!.ToString());
            }
            Assert.Equal(1, store.DeleteCalls);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Fact]
        public async Task EnumerationBelongsToDerivedImplementationAndCanIncludeParentsAndChildren()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root);
            Success(await store.Save("group", "parent value"));
            Success(await store.Save("group/user", "child value"));
            Directory.CreateDirectory(directory.PathFor("group", "internal", "auxiliary"));
            Directory.CreateDirectory(directory.PathFor("only-a-container"));

            var ids = Value(await store.GetAllIds());
            Assert.Equal(new[] { "group", "group/user" }, ids.OrderBy(id => id).ToArray());
            Assert.Equal(1, store.ListCalls);
            Assert.Equal(2, store.EnsureCalls);
            Success(await store.Delete("group"));
            Assert.False(Directory.Exists(directory.PathFor("group", "user")));
            Assert.Empty(Value(await store.GetAllIds()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EnumerationFailureIsNotConvertedToEmptySuccess(bool throwException)
        {
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.Root);
            var store = new Store(() => directory.Root)
            {
                ListFailure = new DirectoryNotFoundException("a child disappeared"),
                ThrowFromList = throwException
            };

            if (throwException)
                Assert.Same(store.ListFailure, await Assert.ThrowsAsync<DirectoryNotFoundException>(() => store.GetAllIds()));
            else
            {
                Assert.False((await store.GetAllIds()).Try(out _, out var failure));
                Assert.Contains("a child disappeared", failure!.ToString());
            }
        }

        [Fact]
        public async Task EachOperationResolvesRootExactlyOnce()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root);
            Success(await store.Save("note", "value"));
            Assert.Equal(1, store.RootCalls);
            Assert.Equal("value", Value(await store.Load("note", new IOException())));
            Assert.Equal(2, store.RootCalls);
            Success(await store.Exists("note"));
            Assert.Equal(3, store.RootCalls);
            Assert.Single(Value(await store.GetAllIds()));
            Assert.Equal(4, store.RootCalls);
            Success(await store.Delete("note"));
            Assert.Equal(5, store.RootCalls);
        }

        [Fact]
        public async Task EnsureAndSaveKeepTheSameResolvedPathWhenEnvironmentChanges()
        {
            using var directory = new FileSystemTestDirectory();
            Directory.CreateDirectory(directory.PathFor("first"));
            Directory.CreateDirectory(directory.PathFor("second"));
            var originalDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = directory.PathFor("first");
                var store = new Store(() => "data")
                {
                    AfterEnsure = _ => Environment.CurrentDirectory = directory.PathFor("second")
                };
                Success(await store.Save("group/user", "value"));
                Assert.Equal(1, store.RootCalls);
                Assert.Equal(store.EnsuredPath, store.SavedPath);
                Assert.Equal("value", File.ReadAllText(directory.PathFor("first", "data", "group", "user", "value.txt")));
                Assert.False(Directory.Exists(directory.PathFor("second", "data")));
                Assert.Empty(Value(await store.GetAllIds()));
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
            }
        }

        [Theory]
        [InlineData(".")]
        [InlineData("")]
        [InlineData("../outside")]
        public async Task InvalidPathsFailBeforeAnyHook(string id)
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.PathFor("root"));
            Assert.False((await store.Save(id, "value")).Try(out _));
            Assert.False((await store.Delete(id)).Try(out _));
            Assert.Empty(store.Events);
            Assert.False(Directory.Exists(directory.Root));
        }

        [Fact]
        public async Task AbsolutePathsAndWrongTargetTypesAreRejected()
        {
            using var directory = new FileSystemTestDirectory();
            var store = new Store(() => directory.Root);
            Assert.False((await store.Save(directory.PathFor("absolute"), "value")).Try(out _));
            Directory.CreateDirectory(directory.Root);
            File.WriteAllText(directory.PathFor("note"), "a file");
            Assert.False((await store.Exists("note")).Try(out _));
            Assert.False((await store.Load("note", new IOException())).Try(out _, out _));
            Assert.Equal(0, store.LoadCalls);
            Assert.False((await store.Delete("note")).Try(out _));
        }

        [Fact]
        public async Task SaveAndLoadExceptionsFromDerivedImplementationPropagate()
        {
            using var directory = new FileSystemTestDirectory();
            var failure = new InvalidOperationException("derived content");
            var store = new Store(() => directory.Root) { ContentException = failure };
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => store.Save("note", "value")));
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => store.Load("note", "")));
        }

        private static void Success(Result result) => Assert.True(result.Try(out var failure), failure?.ToString());

        private static T Value<T>(Result<T> result)
        {
            Assert.True(result.Try(out var value, out var failure), failure?.ToString());
            return value!;
        }

        private sealed class Store(Func<string> root) : DirectoryPersistence<string, string>
        {
            public int RootCalls { get; private set; }
            public int EnsureCalls { get; private set; }
            public int SaveCalls { get; private set; }
            public int LoadCalls { get; private set; }
            public int DeleteCalls { get; private set; }
            public int ListCalls { get; private set; }
            public Exception? ContentException { get; init; }
            public Exception? EnsureFailure { get; init; }
            public Exception? DeleteFailure { get; init; }
            public Exception? ListFailure { get; init; }
            public bool ThrowFromEnsure { get; init; }
            public bool ThrowFromDelete { get; init; }
            public bool ThrowFromList { get; init; }
            public bool FailAfterWrite { get; init; }
            public Action<string>? AfterEnsure { get; init; }
            public string? EnsuredPath { get; private set; }
            public string? SavedPath { get; private set; }
            public List<string> Events { get; } = [];

            public override string GetCurrentRootPath()
            {
                RootCalls++;
                return root();
            }

            protected override string GetRelativeDirectoryPath(string id) => id;

            protected override async Task<Result> EnsureDirectory(string path, string id)
            {
                Events.Add("ensure");
                EnsureCalls++;
                EnsuredPath = path;
                if (ThrowFromEnsure) throw EnsureFailure!;
                if (EnsureFailure != null) return EnsureFailure;
                var result = await base.EnsureDirectory(path, id);
                AfterEnsure?.Invoke(path);
                return result;
            }

            protected override async Task<Result> SaveDirectory(string path, string id, Result<string> value)
            {
                Events.Add("save");
                SaveCalls++;
                if (ContentException != null) throw ContentException;
                SavedPath = path;
                if (!value.Try(out var text, out var failure)) return failure;
                await File.WriteAllTextAsync(Path.Combine(path, "value.txt"), text, TestContext.Current.CancellationToken);
                if (FailAfterWrite) return new IOException("partial save");
                return Result.Success;
            }

            protected override async Task<Result<string>> LoadDirectory(string path, string id, Result<string> oldValue)
            {
                Events.Add("load");
                LoadCalls++;
                if (ContentException != null) throw ContentException;
                return await File.ReadAllTextAsync(Path.Combine(path, "value.txt"), TestContext.Current.CancellationToken);
            }

            protected override Task<Result> DeleteDirectory(string path, string id)
            {
                Events.Add("delete");
                DeleteCalls++;
                if (ThrowFromDelete) throw DeleteFailure!;
                if (DeleteFailure != null) return Task.FromResult<Result>(DeleteFailure);
                return base.DeleteDirectory(path, id);
            }

            protected override Task<Result<string[]>> GetDirectoryIds(string rootPath)
            {
                ListCalls++;
                if (ThrowFromList) throw ListFailure!;
                if (ListFailure != null) return Task.FromResult<Result<string[]>>(ListFailure);
                var ids = Directory.EnumerateFiles(rootPath, "value.txt", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(rootPath, Path.GetDirectoryName(path)!))
                    .Select(path => path.Replace('\\', '/'))
                    .ToArray();
                return Task.FromResult<Result<string[]>>(ids);
            }
        }
    }
}
