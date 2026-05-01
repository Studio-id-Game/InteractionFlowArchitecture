using InteractionFlow.Core.StoragePorts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;

namespace InteractionFlow.Samples.Parrot.StoragePorts
{
    internal interface ILastSelectMemory : IStoragePortModifiable<SampleID?>
    {
    }
}
