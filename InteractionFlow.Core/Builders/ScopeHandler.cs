using System;
using System.Collections.Generic;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 生成済みの DI スコープを保持し、自身と親スコープからサービスを解決するハンドラです。
    /// </summary>
    /// <param name="scope">このハンドラがライフタイムを管理する DI スコープ。</param>
    /// <param name="scopedProvider">このスコープ内でサービス解決を行うプロバイダー。</param>
    /// <param name="parents">自身で解決できない場合に探索する親スコープ。</param>
    public sealed class ScopeHandler(IDisposable scope, IServiceProvider scopedProvider, params ScopeHandler[] parents) : IServiceProvider, IDisposable
    {
        private IDisposable? scope = scope;
        private IServiceProvider? scopedProvider = scopedProvider;

        /// <summary>
        /// このハンドラが管理するスコープが破棄済みかどうかを取得します。
        /// </summary>
        public bool IsDisposed => scope == null;

        /// <summary>
        /// 指定された型のサービスを現在のスコープから解決し、見つからない場合は親スコープを順に探索します。
        /// </summary>
        /// <param name="serviceType">解決するサービスの型。</param>
        /// <returns>解決されたサービス。見つからない場合は <see langword="null"/>。</returns>
        /// <exception cref="InvalidOperationException">このスコープ、または探索対象の親スコープが破棄済みの場合に発生します。</exception>
        public object? GetService(Type serviceType)
        {
            if (scope == null)
                throw new InvalidOperationException(nameof(scope));

            return GetServiceNotVisited(serviceType, []);
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

        /// <summary>
        /// 管理している DI スコープを破棄し、以降のサービス解決を無効にします。
        /// </summary>
        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            scopedProvider = null;
        }
    }
}
