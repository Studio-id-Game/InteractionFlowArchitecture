using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Standard.Externals.Storages.Serializers;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Externals.Serializers
{
    internal sealed class NotepadDataSimpleSerializer : TextSerializer<NotepadData>, INotepadDataSerializerPort
    {
        public override Task<Result<NotepadData>> Deserialize(Result<string> inputText, Result<NotepadData> refValue)
        {
            return Task.FromResult(inputText
                .Then(text =>
                {
                    return refValue.Then(notepad => (text, notepad).AsResult());
                })
                .Then(e =>
                {
                    var (text, notepad) = e;

                    var lines = text.Split('\n').ToList();

                    while (lines.Count < 2)
                    {
                        lines.Add("\n");
                    }

                    notepad.Title = lines[0];
                    notepad.Text = string.Join("\n", lines[1..]);

                    return notepad.AsResult();
                }));
        }

        public override Task<Result<string>> Serialize(Result<NotepadData> inputValue, Result<string> refText)
        {
            return Task.FromResult(inputValue
                .Then(notepad =>
                {
                    return string.Join("\n", notepad.Title, notepad.Text).AsResult();
                }));
        }
    }
}
