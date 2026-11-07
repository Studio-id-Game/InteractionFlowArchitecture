using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Externals;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.Builders;
using System;

namespace InteractionFlow.Samples.Notepad.Core.Builders
{
    public sealed class NotepadProfile : IScopeProfile
    {
        public static IScopeProfile Default { get; } = new NotepadProfile();

        public void Configure(IScopeServices builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            builder.Apply(ConsoleProfile.Default)
                .UseFunction<IConsoleChoiceOperation, ConsoleChoiceOperation>()
                .UseFunction<INoteEditorOperation, ConsoleNoteEditor>()
                .UseInteraction<Login>().UseInteraction<SelectUserAction>().UseInteraction<NoteListView>()
                .UseInteraction<NoteCreate>().UseInteraction<NoteEdit>().UseInteraction<NoteDelete>();
        }
    }
}
