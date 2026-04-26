using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.Interactions
{
    internal class InteractionCanceledException : OperationCanceledException
    {
        public InteractionCanceledException(IInteraction interaction, OperationCanceledException innerException)
            : base($"{interaction.GetName()}: {innerException.Message}", innerException)
        {
            Interaction = interaction;
        }

        public IInteraction Interaction { get; }
    }
}