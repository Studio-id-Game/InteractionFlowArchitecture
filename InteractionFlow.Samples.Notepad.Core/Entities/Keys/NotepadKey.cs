using InteractionFlow.Samples.Notepad.Core.Entities.Rules;

namespace InteractionFlow.Samples.Notepad.Core.Entities.Keys
{
    public readonly record struct NotepadDataKey(string UserId, string NoteId)
    {
        public static NotepadDataKey Empty { get; } = new(new(string.Empty), string.Empty);

        public string UserId { get; } = UserId;
        public string NoteId { get; } = NoteId;

        public readonly bool IsEmpty => this == Empty;

        public bool IsValid => !IsEmpty && NotepadRule.IsValidID(UserId) && NotepadRule.IsValidID(NoteId);

        public NotepadUserKey UserKey => new(UserId);
    }
}
