using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Standard.Externals.Storages.Serializers;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Externals.Serializers
{
    internal class NotepadDataSimpleSerializer : TextSerializer<NotepadData>, INotepadDataSerializerPort
    {
        public override async Task<Result<NotepadData>> Deserialize(Result<string> inputText, Result<NotepadData> refValue)
        {
            if (!inputText)
                return inputText.Exception!;
            if (!refValue)
                return refValue.Exception!;

            var text = inputText.Value!;
            var notepad = refValue.Value!;

            var lines = text.Split('\n').ToList();

            while (lines.Count < 2)
            {
                lines.Add("\n");
            }

            notepad.Title = lines[0];
            notepad.Text = string.Join("\n", lines[1..]);

            return notepad;
        }

        public override async Task<Result<string>> Serialize(Result<NotepadData> inputValue, Result<string> refText)
        {
            if (!inputValue)
                return inputValue.Exception!;

            var notepad = inputValue.Value!;

            return string.Join("\n", notepad.Title, notepad.Text);
        }
    }
}
