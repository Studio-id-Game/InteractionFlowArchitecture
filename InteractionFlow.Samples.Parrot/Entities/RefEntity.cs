namespace InteractionFlow.Samples.Parrot.Entities
{
    internal sealed class RefEntity<T>(T value)
    {
        public T Value { get; set; } = value;
    }
}
