using InteractionFlow.Core.Entities;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public static class IEntryCastComposeExtensions
    {
        public static IEntryCast<TValue, TNextEntry> Compose<TValue, TEntry, TNextEntry>(
            this IEntryCast<TValue, TEntry> first,
            IEntryCast<TEntry, TNextEntry> second)
        {
            return new ComposedEntryCast<TValue, TEntry, TNextEntry>(first, second);
        }

        private sealed class ComposedEntryCast<TValue, TEntry, TNextEntry>(
            IEntryCast<TValue, TEntry> first,
            IEntryCast<TEntry, TNextEntry> second)
            : IEntryCast<TValue, TNextEntry>
        {
            public Result<TNextEntry> GetEntry(Result<TValue> value)
            {
                var firstValue = first.GetEntry(value);
                return second.GetEntry(firstValue);
            }

            public Result<TValue> GetValue(Result<TNextEntry> value)
            {
                var secondValue = second.GetValue(value);
                return first.GetValue(secondValue);
            }
        }
    }
}
