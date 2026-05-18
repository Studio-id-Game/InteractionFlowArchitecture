using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.StoragePorts;

namespace InteractionFlow.Samples.Parrot.StoragePorts
{
    internal interface ILastSelectMemory : IStoragePortModifiable<SampleID?>
    {
    }
}
