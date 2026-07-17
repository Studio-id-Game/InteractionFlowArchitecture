using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;

namespace InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts
{
    internal interface ILastSelectMemory : IStoragePort<bool, RefEntry<SampleID>>
    {
    }
}
