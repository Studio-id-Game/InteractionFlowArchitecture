using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;

namespace InteractionFlow.Samples.Parrot.Externals.Storages
{
    internal class LastSelectMemory : GlobalMemoryModifiable<SampleID?>, ILastSelectMemory
    {
    }
}
