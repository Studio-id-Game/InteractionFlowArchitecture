using System;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Core.Externals
{
    public static class NotepadPaths
    {
        public static string Root(string edition) => Path.Combine(
            Environment.GetEnvironmentVariable("INTERACTIONFLOW_NOTEPAD_ROOT")
                ?? Path.Combine(Environment.CurrentDirectory, "NotepadData-v2"), edition);
    }
}
