using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Parrot.Entities;

namespace InteractionFlow.Samples.Parrot.ExternalPorts
{
    internal interface ILastSelectMemory : IStoragePort<bool, RefEntry<SampleMode>> { }
}
