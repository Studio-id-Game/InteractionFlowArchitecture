using System;
using System.Collections.Generic;

namespace InteractionFlow.Core.Builders
{
    public sealed class ScopeHandler : IServiceProvider, IDisposable
    {
        private IDisposable? scope;
        private IServiceProvider? scopedProvider;
        private readonly ScopeHandler[] parents;

        public bool IsDisposed => scope == null;

        public ScopeHandler(IDisposable scope, IServiceProvider scopedProvider, params ScopeHandler[] parents)
        {
            this.scope = scope;
            this.scopedProvider = scopedProvider;
            this.parents = parents;
        }

        public object? GetService(Type serviceType)
        {
            if (scope == null)
                throw new InvalidOperationException(nameof(scope));

            return GetServiceNotVisited(serviceType, new HashSet<ScopeHandler>());
        }

        private object? GetServiceNotVisited(Type serviceType, HashSet<ScopeHandler> visited)
        {
            // 循環参照対策
            if (!visited.Add(this))
            {
                return null;
            }

            var service = scopedProvider!.GetService(serviceType);

            if (service != null)
            {
                return service;
            }

            foreach (var parent in parents)
            {
                if (parent.IsDisposed) throw new InvalidOperationException();

                service = parent.GetServiceNotVisited(serviceType, visited);

                if (service != null)
                {
                    return service;
                }
            }

            return null;
        }

        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            scopedProvider = null;
        }
    }
}
