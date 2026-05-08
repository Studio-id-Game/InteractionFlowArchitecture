namespace InteractionFlow.Standard.Entities
{
    public interface IClonableState<TSelf>
    {
        public TSelf Copy();
    }
}
