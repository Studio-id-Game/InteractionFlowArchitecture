using Xunit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Tests
{
    public sealed class NotepadProcessTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CreateReloadEditAndDeleteUnicodeNote(bool secure)
        {
            using var app = new Application(secure);
            var created = await app.RunAsync(app.Login(true) + "2\nNote1\nこんにちは🐦\n本文\n..\n.\n0\n");
            Assert.Contains("Created: note1", created);
            var original = File.ReadAllBytes(app.NotePath);
            if (secure) Assert.DoesNotContain("こんにちは", Encoding.UTF8.GetString(original));
            else Assert.Contains("こんにちは🐦\n本文\n.", Encoding.UTF8.GetString(original));

            var edited = await app.RunAsync(app.Login(false) + "1\nnote1\n改訂\nNew body\n.\n0\n");
            Assert.Contains("Current title: こんにちは🐦", edited);
            Assert.Contains("Current body:\n本文\n.", edited);
            Assert.Contains("Saved: note1", edited);
            var reloaded = await app.RunAsync(app.Login(false) + "1\nnote1\n\n/keep\n0\n");
            Assert.Contains("Current title: 改訂", reloaded);
            Assert.Contains("New body", reloaded);

            var deleted = await app.RunAsync(app.Login(false) + "3\nnote1\nno\n3\nnote1\nyes\n0\n");
            Assert.Contains("Delete cancelled.", deleted);
            Assert.Contains("Deleted: note1", deleted);
            Assert.False(File.Exists(app.NotePath));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DuplicateCancelAndEofKeepExistingNote(bool secure)
        {
            using var app = new Application(secure);
            await app.RunAsync(app.Login(true) + "2\nnote1\nOriginal\nBody\n.\n0\n");
            var before = File.ReadAllBytes(app.NotePath);
            var duplicate = await app.RunAsync(app.Login(false) + "2\nNOTE1\n0\n");
            Assert.Contains("already exists", duplicate);
            var cancelled = await app.RunAsync(app.Login(false) + "1\nnote1\nChanged\n/cancel\n0\n");
            Assert.Contains("Original note kept.", cancelled);
            var eof = await app.RunAsync(app.Login(false) + "1\nnote1\nChanged\nIncomplete\n");
            Assert.Contains("Original note kept.", eof);
            Assert.Equal(before, File.ReadAllBytes(app.NotePath));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UserSwitchDoesNotExposeAnotherUsersNotes(bool secure)
        {
            using var app = new Application(secure);
            await app.RunAsync(app.Login(true) + "2\nnote1\nAliceOnly\nSecret body\n.\n0\n");
            var bob = await app.RunAsync("bob\n" + (secure ? "bob-pass\nbob-pass\n" : "") + "0\n");
            Assert.DoesNotContain("AliceOnly", bob);
            var switched = await app.RunAsync(app.Login(false) + "4\nbob\n" + (secure ? "bob-pass\n" : "") + "0\n");
            Assert.DoesNotContain("AliceOnly", switched.Split("Logged in: bob")[1]);
        }

        [Fact]
        public async Task WrongPasswordAndCorruptAccountCannotOverwriteData()
        {
            using var app = new Application(true);
            await app.RunAsync(app.Login(true) + "2\nnote1\nPrivate title\nBody\n.\n0\n");
            var note = File.ReadAllBytes(app.NotePath);
            var account = File.ReadAllBytes(app.AccountPath);
            var failed = await app.RunAsync("alice\nwrong\n/exit\n");
            Assert.Contains("Incorrect password", failed);
            Assert.DoesNotContain("Private title", failed);
            Assert.Equal(account, File.ReadAllBytes(app.AccountPath));
            File.WriteAllText(app.AccountPath, "corrupt");
            var corrupt = await app.RunAsync("alice\nsecret\n/exit\n");
            Assert.Contains("Invalid account file", corrupt);
            Assert.DoesNotContain("New account", corrupt);
            Assert.Equal("corrupt", File.ReadAllText(app.AccountPath));
            Assert.Equal(note, File.ReadAllBytes(app.NotePath));
        }

        [Fact]
        public async Task MissingAccountWithExistingNotesIsNotRecreated()
        {
            using var app = new Application(true);
            await app.RunAsync(app.Login(true) + "2\nnote1\nTitle\nBody\n.\n0\n");
            File.Delete(app.AccountPath);
            var failed = await app.RunAsync("alice\nother-password\n/exit\n");
            Assert.Contains("metadata is missing", failed);
            Assert.False(File.Exists(app.AccountPath));
        }

        [Fact]
        public async Task TamperedCiphertextCannotBeEditedButCanBeDeleted()
        {
            using var app = new Application(true);
            await app.RunAsync(app.Login(true) + "2\nnote1\nTitle\nBody\n.\n0\n");
            var bytes = File.ReadAllBytes(app.NotePath);
            bytes[^1] ^= 1;
            File.WriteAllBytes(app.NotePath, bytes);
            var failed = await app.RunAsync(app.Login(false) + "5\n1\nnote1\n0\n");
            Assert.Contains("Unreadable", failed);
            Assert.Contains("Load failed", failed);
            Assert.Equal(bytes, File.ReadAllBytes(app.NotePath));
            await app.RunAsync(app.Login(false) + "3\nnote1\nyes\n0\n");
            Assert.False(File.Exists(app.NotePath));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InvalidIdCannotEscapeStorageRoot(bool secure)
        {
            using var app = new Application(secure);
            var output = await app.RunAsync(app.Login(true) + "2\n../escape\n0\n");
            Assert.Contains("Invalid note ID", output);
            if (Directory.Exists(app.Root))
                Assert.Empty(Directory.GetFiles(app.Root, "*.note", SearchOption.AllDirectories));
        }

        [Fact]
        public async Task InterruptedOrMismatchedEnrollmentDoesNotCreateAccount()
        {
            using var app = new Application(true);
            var mismatch = await app.RunAsync("alice\nfirst\nsecond\n/exit\n");
            Assert.Contains("Passwords do not match", mismatch);
            Assert.False(File.Exists(app.AccountPath));
            await app.RunAsync("alice\nfirst\n");
            Assert.False(File.Exists(app.AccountPath));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PublicAndNamedPublicAreSeparateUsers(bool secure)
        {
            using var app = new Application(secure);
            var login = "\n" + (secure ? "password\npassword\n" : "");
            await app.RunAsync(login + "2\nnote1\nPublicOnly\nBody\n.\n0\n");
            var namedLogin = "public\n" + (secure ? "password\npassword\n" : "");
            var named = await app.RunAsync(namedLogin + "0\n");
            Assert.DoesNotContain("PublicOnly", named);
        }

        private sealed class Application(bool secure) : IDisposable
        {
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "InteractionFlow-Notepad-" + Guid.NewGuid());
            public string NotePath => Path.Combine(Root, secure ? "secure" : "plain", "user-alice", "note-note1.note");
            public string AccountPath => Path.Combine(Root, "secure-accounts", "user-alice.account");
            public string Login(bool first) => "alice\n" + (secure ? first ? "secret\nsecret\n" : "secret\n" : "");

            public async Task<string> RunAsync(string input)
            {
                var name = secure ? "InteractionFlow.Samples.Notepad.Secure" : "InteractionFlow.Samples.Notepad";
                // Project references copy the executable and runtime files into the test output directory.
                var assembly = Path.Combine(AppContext.BaseDirectory, name + ".dll");
                var start = new ProcessStartInfo("dotnet")
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                start.ArgumentList.Add(assembly);
                start.Environment["INTERACTIONFLOW_NOTEPAD_ROOT"] = Root;
                using var process = Process.Start(start)!;
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                try
                {
                    await process.StandardInput.WriteAsync(input);
                    process.StandardInput.Close();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await process.WaitForExitAsync(timeout.Token);
                    var output = (await stdout).Replace("\r\n", "\n");
                    var errors = await stderr;
                    Assert.True(process.ExitCode == 0, errors + output);
                    Assert.Equal("", errors);
                    return output;
                }
                finally
                {
                    if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
                }
            }

            public void Dispose()
            {
                var target = Path.GetFullPath(Root);
                var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (!target.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
                    || !Path.GetFileName(target).StartsWith("InteractionFlow-Notepad-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected cleanup target.");
                if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
            }
        }
    }
}
