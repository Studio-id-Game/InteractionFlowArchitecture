using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Externals;
using InteractionFlow.Samples.Notepad.Secure.Externals;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Notepad.Tests
{
    public sealed class SerializerTests
    {
        [Fact]
        public async Task PlainSerializerRoundTripsUnicodeAndLeavesStreamsOpen()
        {
            var serializer = new NotepadDataSimpleSerializer();
            using var stream = new MemoryStream();
            var saved = await serializer.Serialize(new NotepadData("日本語🐦", "line1\nline2"), stream.AsResult<Stream>());
            Assert.True(saved.IsSuccess);
            stream.Position = 0;
            var loaded = await serializer.Deserialize(stream.AsResult<Stream>(),
                new InvalidOperationException("No reference value"));
            Assert.True(loaded.HasValue(out var note));
            Assert.Equal("日本語🐦", note!.Title);
            Assert.Equal("line1\nline2", note.Text);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task EncryptedSerializerRoundTripsAndRejectsAnotherUsersSession()
        {
            using var session = new SecureSession();
            var credentialResult = session.CreateCredential("password");
            Assert.True(credentialResult.HasValue(out var credential));
            Assert.True(session.Authenticate("alice", "password", credential!).IsSuccess);
            var serializer = new EncryptedNoteSerializer(session);
            using var stream = new MemoryStream();
            var saved = await serializer.Serialize(new NotepadData("日本語🐦", "Body"), stream.AsResult<Stream>());
            Assert.True(saved.IsSuccess);
            Assert.DoesNotContain("Body", Encoding.UTF8.GetString(stream.ToArray()));
            stream.Position = 0;
            var loaded = await serializer.Deserialize(stream.AsResult<Stream>(), new InvalidOperationException());
            Assert.True(loaded.HasValue(out var note));
            Assert.Equal("日本語🐦", note!.Title);
            Assert.True(stream.CanRead);
            Assert.True(session.Authenticate("bob", "password", credential!).IsSuccess);
            stream.Position = 0;
            var wrongUser = await serializer.Deserialize(stream.AsResult<Stream>(), new InvalidOperationException());
            Assert.False(wrongUser.IsSuccess);
        }

        [Fact]
        public void FailedAuthenticationAndResetDiscardThePreviousKey()
        {
            using var session = new SecureSession();
            Assert.True(session.CreateCredential("secret").HasValue(out var credential));
            Assert.True(session.Authenticate("alice", "secret", credential!).IsSuccess);
            Assert.True(session.Encrypt([1]).HasValue(out _));
            Assert.True(session.Authenticate("alice", "wrong", credential!).HasError(out _));
            Assert.False(session.Encrypt([1]).HasValue(out _));
            Assert.True(session.Authenticate("alice", "secret", credential!).IsSuccess);
            session.ForceResetMemoryState();
            Assert.False(session.Encrypt([1]).HasValue(out _));
        }

        [Fact]
        public async Task FailureInputDoesNotWriteToBorrowedStream()
        {
            using var session = new SecureSession();
            var serializer = new EncryptedNoteSerializer(session);
            using var stream = new MemoryStream([1, 2, 3]);
            var result = await serializer.Serialize(new FormatException("Failed input"), stream.AsResult<Stream>());
            Assert.False(result.IsSuccess);
            Assert.Equal(new byte[] { 1, 2, 3 }, stream.ToArray());
            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public async Task InvalidPlainFormatReturnsFailureInsteadOfAnEmptyNote()
        {
            var serializer = new NotepadDataSimpleSerializer();
            var result = await serializer.Deserialize("No separator".AsResult(), new InvalidOperationException());
            Assert.False(result.IsSuccess);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedSerializationKeepsExistingFileAndRemovesTemporaryFile(bool secure)
        {
            var root = Path.Combine(Path.GetTempPath(), "InteractionFlow-NoteFiles-" + Guid.NewGuid());
            var target = Path.GetFullPath(root);
            var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!target.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("InteractionFlow-NoteFiles-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected cleanup target.");
            using var session = new SecureSession();
            Assert.True(session.CreateCredential("secret").HasValue(out var credential));
            Assert.True(session.Authenticate("alice", "secret", credential!).IsSuccess);
            INotepadDataSerializerPort serializer = secure
                ? new EncryptedNoteSerializer(session) : new NotepadDataSimpleSerializer();
            var files = new TestFiles(serializer, root);
            var id = new NotepadKey("alice", "note");
            try
            {
                Assert.True((await files.Save(id, new NotepadData("Original", "Body"))).IsSuccess);
                var path = Path.Combine(root, "user-alice", "note-note.note");
                var before = File.ReadAllBytes(path);
                var failed = await files.Save(id, new NotepadData("Invalid\nTitle", "Changed"));
                Assert.False(failed.IsSuccess);
                Assert.Equal(before, File.ReadAllBytes(path));
                Assert.Single(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
            }
            finally
            {
                if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
            }
        }

        private sealed class TestFiles(INotepadDataSerializerPort serializer, string root)
            : NotepadDataFilePersistence(serializer)
        {
            public override string GetCurrentRootPath() => root;
        }
    }
}
