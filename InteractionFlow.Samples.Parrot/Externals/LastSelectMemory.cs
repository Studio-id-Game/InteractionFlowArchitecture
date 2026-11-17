using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.ExternalPorts;
using System;
using RefEntry = InteractionFlow.Core.Entities.RefEntry<InteractionFlow.Samples.Parrot.Entities.SampleMode>;

namespace InteractionFlow.Samples.Parrot.Externals
{
    internal sealed class LastSelectMemory : Storage<bool, RefEntry>, ILastSelectMemory
    {
        public override Result<bool> GetKey(IFlowContext context) => true;
        protected override Result CanRemoveValue(bool key, RefEntry value)
            => new InvalidOperationException("Selection memory lasts until the application scope ends.");
        protected override Result<RefEntry> CreateNewValue(bool key)
            => new RefEntry(SampleMode.None);
    }
}
