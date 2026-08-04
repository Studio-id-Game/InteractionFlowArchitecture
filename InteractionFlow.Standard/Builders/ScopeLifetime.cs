using System;
using System.Runtime.ExceptionServices;

namespace InteractionFlow.Standard.Builders
{
    internal sealed class ScopeLifetime(IDisposable scope, IDisposable rootProvider) : IDisposable
    {
        private IDisposable? scope = scope;
        private IDisposable? rootProvider = rootProvider;

        public void Dispose()
        {
            var scope = this.scope;
            var rootProvider = this.rootProvider;

            this.scope = null;
            this.rootProvider = null;

            Exception? scopeException = null;

            try
            {
                scope?.Dispose();
            }
            catch (Exception exception)
            {
                scopeException = exception;
            }

            try
            {
                rootProvider?.Dispose();
            }
            catch (Exception rootProviderException)
            {
                if (scopeException != null)
                {
                    throw new AggregateException(scopeException, rootProviderException);
                }

                ExceptionDispatchInfo.Capture(rootProviderException).Throw();
            }

            if (scopeException != null)
            {
                ExceptionDispatchInfo.Capture(scopeException).Throw();
            }
        }
    }
}
