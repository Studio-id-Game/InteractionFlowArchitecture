using InteractionFlow.Samples.Notepad.Core.Entities.Rules;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Keys
{
    public readonly record struct NotepadUserKey(string Id)
    {
        public static NotepadUserKey Public => new("");

        public string Id { get; } = Id;

        public bool IsPublic => string.IsNullOrWhiteSpace(Id);

        public bool IsValid => IsPublic || NotepadRule.IsValidID(Id);

        public string Name => IsPublic ? NotepadRule.PublicUserName : Id;
    }
}
