using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.ExternalPorts;
using System;

namespace InteractionFlow.Samples.Parrot.Externals
{
    internal sealed class LastSelectMemory : Storage<bool, RefEntry<SampleMode>>, ILastSelectMemory
    {
        public override Result<bool> GetKey(IFlowContext context) => true;
        protected override Result CanRemoveValue(bool key, RefEntry<SampleMode> value)
            => new InvalidOperationException("Selection memory lasts until the application scope ends.");
        protected override Result<RefEntry<SampleMode>> CreateNewValue(bool key)
            => new RefEntry<SampleMode>(SampleMode.None);
    }
}
