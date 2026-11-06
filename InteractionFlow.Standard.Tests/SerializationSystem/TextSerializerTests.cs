using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.SerializationSystem.Externals;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.SerializationSystem
{
    public sealed class TextSerializerTests
    {
        [Fact]
        public async Task DefaultEncodingRoundTripsWithoutBomAndLeavesStreamOpen()
        {
            var serializer = new Probe();
            using var stream = new MemoryStream();
            Assert.Same(stream, Value(await serializer.Serialize("日本語".AsResult(), stream)));
            Assert.Equal(new UTF8Encoding(false, true).GetBytes("日本語"), stream.ToArray());
            Assert.Equal("", Value(serializer.ReferenceText));
            stream.Position = 0;
            var missing = new IOException("no previous value");
            Assert.Equal("日本語", Value(await serializer.Deserialize(stream.AsResult<Stream>(), missing)));
            Assert.Same(missing, Error(serializer.ReferenceValue));
            Assert.Equal(stream.Length, stream.Position);
            Assert.True(stream.CanRead);
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public async Task FailedDestinationSkipsConversion()
        {
            var serializer = new Probe();
            var failure = new IOException("destination");
            Assert.Same(failure, Error(await serializer.Serialize("text".AsResult(), (Result<Stream>)failure)));
            Assert.Equal(0, serializer.SerializeCalls);
        }

        [Fact]
        public async Task FailedSourceSkipsConversion()
        {
            var serializer = new Probe();
            var failure = new IOException("source");
            Assert.Same(failure, Error(await serializer.Deserialize((Result<Stream>)failure, "previous")));
            Assert.Equal(0, serializer.DeserializeCalls);
        }

        [Fact]
        public async Task FailedValueIsPassedToConversionWithoutWriting()
        {
            var serializer = new Probe();
            using var stream = new MemoryStream();
            var failure = new InvalidOperationException("value");
            Assert.Same(failure, Error(await serializer.Serialize((Result<string>)failure, stream)));
            Assert.Equal(1, serializer.SerializeCalls);
            Assert.Empty(stream.ToArray());
            Assert.True(stream.CanWrite);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConversionExceptionsPropagate(bool asynchronous)
        {
            var failure = new InvalidOperationException("conversion");
            var serializer = new Probe { Failure = failure, ThrowAsynchronously = asynchronous };
            using var stream = new MemoryStream(new byte[] { 65 });
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(
                () => serializer.Serialize("text".AsResult(), stream)));
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(
                () => serializer.Deserialize(stream.AsResult<Stream>(), "previous")));
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task InvalidUtf8AndUnpairedSurrogatesFail()
        {
            var serializer = new Probe();
            using var input = new MemoryStream(new byte[] { 0xff });
            Assert.IsType<DecoderFallbackException>(Error(await serializer.Deserialize(input.AsResult<Stream>(), "")));
            Assert.Equal(0, serializer.DeserializeCalls);
            using var output = new MemoryStream();
            Assert.IsType<EncoderFallbackException>(Error(await serializer.Serialize("\ud800".AsResult(), output)));
            Assert.True(input.CanRead);
            Assert.True(output.CanWrite);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BomDetectionCanBeDisabled(bool detect)
        {
            var serializer = new Probe { DetectBom = detect };
            var encoding = new UnicodeEncoding(false, true, true);
            using var stream = new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes("text")).ToArray());
            var result = await serializer.Deserialize(stream.AsResult<Stream>(), "");
            if (detect) Assert.Equal("text", Value(result));
            else Assert.IsType<DecoderFallbackException>(Error(result));
        }

        [Fact]
        public async Task EncodingControlsOutputPreambleAndFallback()
        {
            var encoding = new UnicodeEncoding(false, true, true);
            var serializer = new Probe { SelectedEncoding = encoding };
            using var stream = new MemoryStream();
            Value(await serializer.Serialize("日本語".AsResult(), stream));
            Assert.Equal(encoding.GetPreamble().Concat(encoding.GetBytes("日本語")).ToArray(), stream.ToArray());
            stream.Position = 0;
            Assert.Equal("日本語", Value(await serializer.Deserialize(stream.AsResult<Stream>(), "")));
            var replacing = new Probe { SelectedEncoding = new UTF8Encoding(false, false) };
            using var invalid = new MemoryStream(new byte[] { 0xff });
            Assert.Equal("\ufffd", Value(await replacing.Deserialize(invalid.AsResult<Stream>(), "")));
        }

        [Fact]
        public async Task WritingUsesCurrentPositionWithoutTruncatingAndReadingConsumesRemainder()
        {
            var serializer = new Probe();
            using var stream = new MemoryStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes("abcdef"), TestContext.Current.CancellationToken);
            stream.Position = 2;
            Value(await serializer.Serialize("X".AsResult(), stream));
            Assert.Equal("abXdef", Encoding.UTF8.GetString(stream.ToArray()));
            stream.Position = 2;
            Assert.Equal("Xdef", Value(await serializer.Deserialize(stream.AsResult<Stream>(), "")));
            Assert.Equal(stream.Length, stream.Position);
        }

        [Fact]
        public async Task UnavailableStreamsReturnIoFailures()
        {
            var serializer = new Probe();
            var stream = new MemoryStream();
            stream.Dispose();
            Error(await serializer.Serialize("text".AsResult(), stream));
            Error(await serializer.Deserialize(stream.AsResult<Stream>(), ""));
            Assert.Equal(0, serializer.DeserializeCalls);
        }

        private static T Value<T>(Result<T> result)
        {
            Assert.True(result.Try(out var value, out var error), error?.ToString());
            return value;
        }

        private static Exception Error<T>(Result<T> result)
        {
            Assert.False(result.Try(out _, out var error));
            return error.GetBaseException();
        }

        private sealed class Probe : TextSerializer<string>
        {
            public Encoding? SelectedEncoding { get; init; }
            public bool DetectBom { get; init; } = true;
            public Exception? Failure { get; init; }
            public bool ThrowAsynchronously { get; init; }
            public int SerializeCalls { get; private set; }
            public int DeserializeCalls { get; private set; }
            public Result<string> ReferenceText { get; private set; }
            public Result<string> ReferenceValue { get; private set; }
            public override Encoding Encoding => SelectedEncoding ?? base.Encoding;
            public override bool DetectEncodingFromByteOrderMarks => DetectBom;

            public override Task<Result<string>> Serialize(Result<string> value, Result<string> reference)
            {
                SerializeCalls++;
                ReferenceText = reference;
                return Convert(value);
            }

            public override Task<Result<string>> Deserialize(Result<string> text, Result<string> reference)
            {
                DeserializeCalls++;
                ReferenceValue = reference;
                return Convert(text);
            }

            private Task<Result<string>> Convert(Result<string> value)
            {
                if (Failure is null) return Task.FromResult(value);
                if (ThrowAsynchronously) return Task.FromException<Result<string>>(Failure);
                throw Failure;
            }
        }
    }
}
