namespace InteractionFlow.Core.Builders
{
    public interface IScopeBuilder : IScopeServices
    {
        ScopeHandler BuildScope(params ScopeHandler[] parents);
    }
}
