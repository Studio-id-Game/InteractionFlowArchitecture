namespace InteractionFlow.Core.Entities.Rules.Architectures
{
    public static class IFlowNodeExtensions
    {
        public static string GetName<T>(this T @this) where T : IFlowNode => @this.Name;

        public static FlowLayerTypes GetLayer<T>(this T @this) where T : IFlowNode => @this.Layer;

        public static FunctionPortTypes GetFunctionTypes<T>(this T @this) where T : IFlowNode => @this.FunctionTypes;
    }
}
