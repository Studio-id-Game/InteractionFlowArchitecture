using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts.SilentExternalPorts;
using System;

namespace InteractionFlow.Core.Externals.SilentExternals
{
    /// <summary>
    /// SilentExternal ポートのデフォルト実装基底クラスです。
    /// </summary>
    public abstract class SilentExternal : ISilentExternalPort
    {
        private readonly IDependencyNode[] dependency;

        /// <summary>
        /// 依存ノードを保持し、派生クラスの状態を初期化します。
        /// </summary>
        /// <param name="dependency">この SilentExternal が依存するフローノード。</param>
        public SilentExternal(params IDependencyNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        /// <summary>
        /// この SilentExternal が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IDependencyNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        /// <summary>
        /// 派生クラスが保持するメモリ上の状態を初期化します。
        /// </summary>
        public abstract void ForceResetMemoryState();
    }
}
