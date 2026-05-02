using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Standard.Storages;

namespace InteractionFlow.Samples.Parrot.Storages
{
    internal class LastSelectMemory : GlobalMemoryModifiable<SampleID?>, ILastSelectMemory
    {
    }
}
