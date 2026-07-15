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
        private readonly IDependencyNode[] dependency;

        /// <summary>
        /// 依存ノードを保持します。派生クラスの状態初期化は派生クラスのコンストラクタで行います。
        /// </summary>
        /// <param name="dependency">この Operation が依存するフローノード。</param>
        public Operation(params IDependencyNode[] dependency)
        {
            this.dependency = dependency;
        }

        /// <summary>
        /// この Operation が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IDependencyNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;

        /// <summary>
        /// 派生クラスが保持するメモリ上の状態を初期化します。
        /// </summary>
        public abstract void ForceResetMemoryState();
    }
}
