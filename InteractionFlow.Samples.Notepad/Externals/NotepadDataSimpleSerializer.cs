using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.SerializationSystem.Externals;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Externals
{
    public sealed class NotepadDataSimpleSerializer : TextSerializer<NotepadData>, INotepadDataSerializerPort
    {
        public override Task<Result<string>> Serialize(Result<NotepadData> inputValue, Result<string> refText)
            => inputValue.Then(note => Validate(note)
                .Then(() => (note.Title + "\n" + note.Text).AsResult())).StartAsync();

        public override Task<Result<NotepadData>> Deserialize(Result<string> inputText, Result<NotepadData> refValue)
            => inputText.Then(text =>
            {
                var separator = text.IndexOf('\n');
                if (separator < 0) return new FormatException("Note has no title separator.");
                var note = new NotepadData(text.Substring(0, separator).TrimEnd('\r'), text.Substring(separator + 1));
                return Validate(note).Then(() => note.AsResult());
            }).StartAsync();

        private static Result Validate(NotepadData note)
            => note.Title.Length > NotepadRules.MaxTitleLength || note.Title.IndexOfAny(new[] { '\r', '\n' }) >= 0
                || note.Text.Length > NotepadRules.MaxTextLength
                ? new FormatException("Invalid note size or title.") : Result.Success;
    }
}
