using System;
using System.Diagnostics;

namespace InteractionFlow.Core.Entities
{
    public class ResultException : Exception
    {
        internal ResultException(Exception inner) : base($"ResultMessage : {inner.Message}", inner)
        {
        }

        internal ResultException() : base($"ResultMessage : Invalid Result Exception")
        {

        }

# if DEBUG
        public StackTrace? ResultCreationStackTrace { get; } = new(3, true);
#else
        public StackTrace? ResultCreationStackTrace { get; } = null;
#endif
    }
}
