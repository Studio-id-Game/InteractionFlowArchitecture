using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.StoragePorts;
using System;
using System.Collections;
using System.Collections.Generic;

namespace InteractionFlow.Core.Externals.Storages
{
    /// <summary>
    /// キーと値をメモリ上の辞書で管理する Storage ポートのデフォルト実装基底クラスです。
    /// </summary>
    /// <remarks>
    /// この実装は <see cref="GetOrCreate(TKey)"/> で作成した値を登録し、Storage が所有するメモリーキャッシュとして扱います。
    /// 登録済みの値が <see cref="IDisposable"/> を実装している場合、削除時に破棄するか、登録だけ解除するかを選択できます。
    /// </remarks>
    /// <typeparam name="TKey">状態を識別するキーの型。</typeparam>
    /// <typeparam name="TValue">保持する値の型。</typeparam>
    public abstract class Storage<TKey, TValue> : IStoragePort<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IDisposable
    {
        private readonly IDependencyNode[] dependency;
        private readonly Dictionary<TKey, TValue> items;

        /// <summary>
        /// キー比較方法と依存ノードを指定して、空のメモリー Storage を作成します。
        /// 派生クラスの状態初期化は派生クラスのコンストラクタで行います。
        /// </summary>
        /// <param name="comparer">キー比較に使用する比較器。<see langword="null"/> の場合は既定の比較器を使用します。</param>
        /// <param name="dependency">この Storage が依存するフローノード。</param>
        public Storage(IEqualityComparer<TKey>? comparer = null, params IDependencyNode[] dependency)
        {
            items = new(comparer);
            this.dependency = dependency;
        }

        /// <summary>
        /// この Storage が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlyMemory<IDependencyNode> Dependency => dependency;

        // IStoragePort<TKey, TValue>
        #region IStoragePort<TKey, TValue>

        /// <summary>
        /// 現在保持しているキーと値の数を取得します。
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// 保持しているすべての値を、破棄せずに登録から削除します。
        /// </summary>
        /// <returns>
        /// すべての値を削除できた場合は成功結果。
        /// 削除できない値がある場合は、各失敗を <see cref="AggregateException"/> に集約した失敗結果。
        /// </returns>
        /// <remarks>
        /// <see cref="CanRemoveValue(TKey, TValue)"/> 自身が例外を送出した場合、その例外は集約せずに伝播します。
        /// </remarks>
        public Result ClearWithoutDispose()
        {
            var canRemove = CanRemoveAllItems();
            if (!canRemove.Try(out var e))
            {
                return e;
            }

            items.Clear();
            return Result.Success;
        }

        /// <summary>
        /// 保持しているすべての値を登録から削除し、破棄可能な値は破棄します。
        /// </summary>
        /// <remarks>
        /// <see cref="CanRemoveValue(TKey, TValue)"/> 自身が例外を送出した場合、その例外は集約せずに伝播します。
        /// 破棄中に例外が発生した場合も、すべての値について破棄を試み、登録状態を初期化してから例外を送出します。
        /// </remarks>
        /// <returns>
        /// すべての値を削除できた場合は成功結果。
        /// 削除できない値がある場合は、各失敗を <see cref="AggregateException"/> に集約した失敗結果。
        /// </returns>
        /// <exception cref="AggregateException">保持値の破棄中に 1 つ以上の例外が発生した場合。</exception>
        public Result ClearAndDispose()
        {
            var canRemove = CanRemoveAllItems();
            if (!canRemove.Try(out var e))
            {
                return e;
            }

            ForceResetItems();
            return Result.Success;
        }

        private Result CanRemoveAllItems()
        {
            List<Exception>? exceptions = null;

            foreach (var (key, value) in items)
            {
                if (!CanRemoveValue(key, value).Try(out var e))
                {
                    exceptions ??= [];
                    exceptions.Add(e.InnerException);
                }
            }

            return exceptions == null
                ? Result.Success
                : new AggregateException(exceptions);
        }

        /// <summary>
        /// 指定されたキーに対応する値を保持しているかどうかを判定します。
        /// </summary>
        /// <param name="key">確認するキー。</param>
        /// <returns>キーに対応する値を保持している場合は <see langword="true"/>。</returns>
        public bool ContainsKey(TKey key)
        {
            return items.ContainsKey(key);
        }

        /// <summary>
        /// 保持している値を破棄可能であれば破棄し、メモリ上の登録状態を強制的に初期化します。
        /// </summary>
        /// <remarks>
        /// このメソッドは強制リセットとして、<see cref="CanRemoveValue(TKey, TValue)"/> による削除可否判定を行いません。
        /// 保持している値を直接走査し、<see cref="IDisposable"/> を実装する値を破棄してから登録をすべて削除します。
        /// 破棄時に発生した例外は集約して送出しますが、その場合も登録状態は初期化されます。
        /// </remarks>
        /// <exception cref="AggregateException">保持値の破棄中に 1 つ以上の例外が発生した場合。</exception>
        public virtual void ForceResetMemoryState()
        {
            ForceResetItems();
        }

        private void ForceResetItems()
        {
            List<Exception>? exceptions = null;

            try
            {
                foreach (var value in items.Values)
                {
                    if (value is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception e)
                        {
                            exceptions ??= [];
                            exceptions.Add(e);
                        }
                    }
                }
            }
            finally
            {
                items.Clear();
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// 指定されたキーに対応する値を取得します。
        /// </summary>
        /// <param name="key">取得する値のキー。</param>
        /// <returns>キーに対応する値。存在しない場合は失敗結果。</returns>
        public Result<TValue> Get(TKey key)
        {
            if (items.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                return new KeyNotFoundException(key?.ToString());
            }
        }

        /// <summary>
        /// 指定されたコンテキストから <typeparamref name="TKey"/> 型の値を取得し、Storage のキーとして返します。
        /// </summary>
        /// <param name="context">キーを取得するフローコンテキスト。</param>
        /// <returns>取得できたキー。取得できない場合は失敗結果。</returns>
        public virtual Result<TKey> GetKey(IFlowContext context)
        {
            if (context.TryGet<TKey>(out var key) && key != null)
            {
                return key;
            }
            else
            {
                return new KeyNotFoundException(nameof(TKey));
            }
        }

        /// <summary>
        /// 指定されたキーに対応する値を取得し、存在しない場合は派生クラスの作成処理で新しい値を追加します。
        /// </summary>
        /// <param name="key">取得または作成する値のキー。</param>
        /// <returns>取得または作成された値。作成に失敗した場合は失敗結果。</returns>
        /// <remarks>
        /// 新しく作成された値は内部辞書へ登録され、この Storage が所有する値として扱われます。
        /// </remarks>
        public Result<TValue> GetOrCreate(TKey key)
        {
            return Get(key)
                .ThenError(_ =>
                {
                    //OnSuccess() を入れ子にしているのは、CreateNewValue() のルートでのみ items.Add() を実行するため
                    return CreateNewValue(key)
                        .OnSuccess(newValue =>
                        {
                            items.Add(key, newValue);
                        });
                });
        }

        /// <summary>
        /// 指定されたキーの値を、破棄せずに登録から削除します。
        /// </summary>
        /// <param name="key">削除する値のキー。</param>
        /// <returns>削除に成功した場合は成功結果。キーが存在しない場合や削除できない場合は失敗結果。</returns>
        /// <remarks>
        /// <see cref="CanRemoveValue(TKey, TValue)"/> 自身が例外を送出した場合、その例外は伝播します。
        /// </remarks>
        public Result RemoveWithoutDispose(TKey key)
        {
            if (items.TryGetValue(key, out var value))
            {
                return CanRemoveValue(key, value)
                    .OnSuccess(() =>
                    {
                        items.Remove(key);
                    });
            }
            else
            {
                return new KeyNotFoundException();
            }
        }

        /// <summary>
        /// 指定されたキーの値を登録から削除し、破棄可能な値は破棄します。
        /// </summary>
        /// <param name="key">削除する値のキー。</param>
        /// <returns>削除に成功した場合は成功結果。キーが存在しない場合や削除できない場合は失敗結果。</returns>
        /// <remarks>
        /// <see cref="CanRemoveValue(TKey, TValue)"/> 自身が例外を送出した場合、その例外は伝播します。
        /// 値の破棄で例外が発生した場合、その値は既に Storage の登録から削除されています。
        /// </remarks>
        /// <exception cref="Exception">登録解除後、保持値の破棄中に例外が発生した場合。</exception>
        public Result RemoveAndDispose(TKey key)
        {
            if (items.TryGetValue(key, out var value))
            {
                return CanRemoveValue(key, value)
                    .OnSuccess(() =>
                    {
                        items.Remove(key);

                        if (value is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    });
            }
            else
            {
                return new KeyNotFoundException();
            }
        }

        /// <summary>
        /// 指定された値を削除してよいかを派生クラスで判定します。
        /// </summary>
        /// <param name="key">削除候補の値のキー。</param>
        /// <param name="value">削除候補の値。</param>
        /// <returns>削除可能な場合は成功結果。削除できない場合は失敗結果。</returns>
        protected abstract Result CanRemoveValue(TKey key, TValue value);

        /// <summary>
        /// 指定されたキーに対応する新しい値を派生クラスで作成します。
        /// </summary>
        /// <param name="key">作成する値のキー。</param>
        /// <returns>作成された値。作成できない場合は失敗結果。</returns>
        /// <remarks>
        /// 成功した値は <see cref="GetOrCreate(TKey)"/> によって Storage へ登録され、Storage が所有する値として扱われます。
        /// </remarks>
        protected abstract Result<TValue> CreateNewValue(TKey key);

        /// <summary>
        /// 保持しているキーと値の列挙子を取得します。
        /// </summary>
        /// <returns>保持しているキーと値の列挙子。</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// この Storage が所有するマネージド状態を破棄します。
        /// </summary>
        /// <param name="disposing">
        /// マネージド状態を破棄する場合は <see langword="true"/>。それ以外の場合は <see langword="false"/>。
        /// </param>
        /// <remarks>
        /// <paramref name="disposing"/> が <see langword="true"/> の場合、
        /// 保持値を破棄して登録状態を強制的に初期化します。
        /// </remarks>
        /// <exception cref="AggregateException">保持値の破棄中に 1 つ以上の例外が発生した場合。</exception>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ForceResetItems();
            }
        }

        /// <summary>
        /// 保持している値を破棄し、メモリ上の登録状態を強制的に初期化します。
        /// </summary>
        /// <remarks>
        /// 現在の実装は、破棄後の再利用を明示的には拒否しません。
        /// </remarks>
        /// <exception cref="AggregateException">保持値の破棄中に 1 つ以上の例外が発生した場合。</exception>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
