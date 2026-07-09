using InteractionFlow.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries;

namespace InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts
{
    internal interface ILastSelectMemory : IStoragePort<bool, RefEntry<SampleID>>
    {
    }
}
