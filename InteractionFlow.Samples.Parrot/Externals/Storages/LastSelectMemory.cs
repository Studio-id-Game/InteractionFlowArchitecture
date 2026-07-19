using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Storages;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts;
using System;

namespace InteractionFlow.Samples.Parrot.Externals.Storages
{
    internal sealed class LastSelectMemory : Storage<bool, RefEntry<SampleID>>, ILastSelectMemory
    {
        public override Result<bool> GetKey(IFlowContext context)
        {
            return true;
        }

        protected override Result CanRemoveValue(bool key, RefEntry<SampleID> value)
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception e)
            {
                return e;
            }
        }

        protected override Result<RefEntry<SampleID>> CreateNewValue(bool key)
        {
            try
            {
                if (key)
                {
                    return new RefEntry<SampleID>(new SampleID(SampleMode.None));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
