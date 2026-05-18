namespace InteractionFlow.Standard.Entities
{
    public interface IHasFunctionState<TState>
        where TState : IFunctionState<TState>
    {
        TState State { get; set; }
    }
}
