namespace InteractionFlow.Standard.Entities
{
    public static class FunctionStateScopeExtention
    {
        public static FunctionStateScope<TState> GetStateScope<TState>(this IHasFunctionState<TState> target)
            where TState : IFunctionState<TState>
        {
            return new(target);
        }
    }
}
