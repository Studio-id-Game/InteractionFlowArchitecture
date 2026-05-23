using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts
{
    internal interface ILastSelectMemory : IStoragePortModifiable<SampleID?>
    {
    }
}
