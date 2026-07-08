using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using System;

namespace InteractionFlow.Core.Externals.Operations
{
    /// <summary>
    /// Operation ポートのデフォルト実装基底クラスです。
    /// </summary>
    public abstract class Operation : IOperationPort
    {
        private readonly IFlowNode[] dependency;

        /// <summary>
        /// 依存ノードを保持し、派生クラスの状態を初期化します。
        /// </summary>
        /// <param name="dependency">この Operation が依存するフローノード。</param>
        public Operation(params IFlowNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        /// <summary>
        /// この Operation が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;

        /// <summary>
        /// 派生クラスが保持するメモリ上の状態を初期化します。
        /// </summary>
        public abstract void ForceResetMemoryState();
    }
}
