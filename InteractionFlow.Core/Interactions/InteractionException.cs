using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.Interactions
{
    internal class InteractionException : Exception
    {
        public InteractionException(IInteraction interaction, Exception innerException)
            : base($"{interaction.GetName()}: {innerException.Message}", innerException)
        {
            Interaction = interaction;
        }

        public IInteraction Interaction { get; }
    }
}