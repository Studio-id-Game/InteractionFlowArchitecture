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
    public abstract class Storage<TKey, TValue> : IStoragePort<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
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
        /// <returns>すべての値を削除できた場合は成功結果。削除できない値がある場合は失敗結果。</returns>
        public Result ClearWithoutDispose()
        {
            foreach (var (key, value) in items)
            {
                if (!CanRemoveValue(key, value).Try(out var e))
                {
                    return e;
                }
            }

            items.Clear();
            return Result.Success;
        }

        /// <summary>
        /// 保持しているすべての値を登録から削除し、破棄可能な値は破棄します。
        /// </summary>
        /// <returns>すべての値を削除できた場合は成功結果。削除できない値がある場合は失敗結果。</returns>
        public Result ClearAndDispose()
        {
            foreach (var (key, value) in items)
            {
                if (!CanRemoveValue(key, value).Try(out var e))
                {
                    return e;
                }
            }

            foreach (var value in items.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            items.Clear();
            return Result.Success;
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
        /// 保持している値を破棄可能であれば破棄し、メモリ上の登録状態を初期化します。
        /// </summary>
        public virtual void ForceResetMemoryState()
        {
            ClearAndDispose();
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
    }
}
