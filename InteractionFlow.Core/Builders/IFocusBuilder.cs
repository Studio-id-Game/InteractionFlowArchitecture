using InteractionFlow.Core.Focuses;
using InteractionFlow.Core.OperationPorts;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Core.StoragePorts;

namespace InteractionFlow.Core.Builders
{
    public interface IFocusBuilder
    {
        IFocusBuilder AddOperation<TInput, TImpl>()
            where TImpl : IOperationPort<TInput>;

        IFocusBuilder AddStorage<TValue, TImpl>()
            where TImpl : IStoragePort<TValue>;

        IFocusBuilder AddReaction<TOutput, TImpl>()
            where TImpl : IReactionPort<TOutput>;

        TFocus Build<TFocus>()
            where TFocus : IFocus;
    }
}
