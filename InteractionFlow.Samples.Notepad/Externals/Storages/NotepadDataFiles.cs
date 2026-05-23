using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Externals.Storages
{
    internal class NotepadDataFiles(INotepadDataMemory memory)
        : FileStorageModifiable<NotepadData, INotepadDataMemory>(memory), INotepadDataFiles
    {
        protected override FileInfo? GetFileInfo(IFlowContext context)
        {
            if (context.TryGet(out NotepadDataKey key))
            {
                return key.GetNoteFile();
            }

            return null;
        }

        protected override async Task<Result<NotepadData>> ReadFileAsync(IFlowContext context, FileInfo file)
        {
            try
            {
                using var reader = file.OpenRead();
                using var textReader = new StreamReader(reader, Encoding.UTF8);

                var title = await textReader.ReadLineAsync() ?? "";
                var text = await textReader.ReadToEndAsync();
                var dataKey = NotepadDataKey.CreateFromNoteFile(file);
                if (dataKey.HasValue)
                {
                    return new NotepadData(dataKey.Value)
                    {
                        Title = title,
                        Text = text,
                    };
                }
                else
                {
                    return new InvalidOperationException($"Invalid FileInfo '{file.FullName}'");
                }
            }
            catch (Exception e)
            {
                return e;
            }
        }

        protected override async Task<Result> WriteFileAsync(IFlowContext context, FileInfo file, NotepadData value)
        {
            try
            {
                MakeDirectory(file.Directory);

                var tempFile = new FileInfo(Path.ChangeExtension(file.FullName, ".temp"));

                using (var writer = tempFile.Create())
                using (var textWriter = new StreamWriter(writer, Encoding.UTF8))
                {
                    await textWriter.WriteLineAsync(value.Title);
                    await textWriter.WriteAsync(value.Text);
                }

                File.Move(tempFile.FullName, file.FullName, true);

                return true;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        protected override Result<NotepadData> ValidateNormalize(NotepadData value)
        {
            if (value.NoteKey.IsValid)
            {
                value.Title = value.Title.Trim(' ', '\n', '\t', '\r');
                return value;
            }
            else
            {
                return new InvalidOperationException("Invalid NotepadData");
            }
        }

        public void Clear()
        {
            CacheStorage.Clear();
        }
    }
}
