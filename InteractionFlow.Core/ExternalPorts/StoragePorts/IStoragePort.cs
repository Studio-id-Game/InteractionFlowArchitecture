using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Collections.Generic;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts
{
    /// <summary>
    /// 状態の保存・読み込み・管理を担当する Storage ポートを表します。
    /// </summary>
    public interface IStoragePort : IFlowNodeStateful
    {
        /// <summary>
        /// Storage ポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが Storage 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        /// <summary>
        /// 保持しているすべての値を削除し、破棄可能な値は破棄します。
        /// </summary>
        /// <returns>削除に成功した場合は成功結果、削除できない値がある場合は失敗結果。</returns>
        Result ClearAndDispose();

        /// <summary>
        /// 保持しているすべての値を、破棄せずに削除します。
        /// </summary>
        /// <returns>削除に成功した場合は成功結果、削除できない値がある場合は失敗結果。</returns>
        Result ClearWithoutDispose();
    }

    /// <summary>
    /// コンテキストからキーを解決し、キー単位で状態を管理する Storage ポートを表します。
    /// </summary>
    /// <typeparam name="TKey">状態を識別するキーの型。</typeparam>
    public interface IStoragePort<TKey> : IStoragePort
    {
        /// <summary>
        /// 指定されたコンテキストから、この Storage で使用するキーを取得します。
        /// </summary>
        /// <param name="context">キーを取得するフローコンテキスト。</param>
        /// <returns>取得できたキー。取得できない場合は失敗結果。</returns>
        Result<TKey> GetKey(IFlowContext context);

        /// <summary>
        /// 指定されたキーの値を保持しているかどうかを判定します。
        /// </summary>
        /// <param name="key">確認するキー。</param>
        /// <returns>キーに対応する値を保持している場合は <see langword="true"/>。</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// 指定されたキーの値を削除し、破棄可能な値は破棄します。
        /// </summary>
        /// <param name="key">削除する値のキー。</param>
        /// <returns>削除に成功した場合は成功結果、キーが存在しない場合や削除できない場合は失敗結果。</returns>
        Result RemoveAndDispose(TKey key);

        /// <summary>
        /// 指定されたキーの値を、破棄せずに削除します。
        /// </summary>
        /// <param name="key">削除する値のキー。</param>
        /// <returns>削除に成功した場合は成功結果、キーが存在しない場合や削除できない場合は失敗結果。</returns>
        Result RemoveWithoutDispose(TKey key);
    }

    /// <summary>
    /// キーと値の組を保持し、読み取り専用コレクションとして列挙できる Storage ポートを表します。
    /// </summary>
    /// <typeparam name="TKey">状態を識別するキーの型。</typeparam>
    /// <typeparam name="TValue">保持する値の型。</typeparam>
    public interface IStoragePort<TKey, TValue> : IStoragePort<TKey>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// 指定されたキーに対応する値を取得します。
        /// </summary>
        /// <param name="key">取得する値のキー。</param>
        /// <returns>キーに対応する値。存在しない場合は失敗結果。</returns>
        Result<TValue> Get(TKey key);

        /// <summary>
        /// 指定されたキーに対応する値を取得し、存在しない場合は新しい値を作成して取得します。
        /// </summary>
        /// <param name="key">取得または作成する値のキー。</param>
        /// <returns>取得または作成された値。作成に失敗した場合は失敗結果。</returns>
        Result<TValue> GetOrCreate(TKey key);
    }
}
