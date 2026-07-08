using System;
using System.Collections.Generic;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// 既存のコンテキストに一時的な値を重ねて扱うコンテキストです。
    /// </summary>
    /// <param name="mainContext">値の探索先となる元のコンテキスト。</param>
    public class FlowContextGroup(IFlowContext mainContext) : IFlowContext
    {
        private readonly List<IFlowContextValue> immutableValues = [];
        private readonly List<IFlowContextValue> values = [];

        /// <summary>
        /// 元のコンテキストに紐づくユーザー情報を取得します。
        /// </summary>
        public UserObject User => mainContext.User;

        /// <summary>
        /// 元のコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation => mainContext.Cancellation;

        /// <summary>
        /// 読み取り専用の一時値を追加します。
        /// </summary>
        /// <typeparam name="T">追加する値の型。</typeparam>
        /// <param name="value">追加する値。</param>
        /// <param name="contextValue">追加された値オブジェクト。</param>
        /// <returns>現在のコンテキストグループ。</returns>
        public FlowContextGroup AddImmutable<T>(T value, out FlowContextValueImmutable<T> contextValue)
        {
            contextValue = new FlowContextValueImmutable<T>(value);
            immutableValues.Insert(0, contextValue);

            return this;
        }

        /// <summary>
        /// 取得と更新が可能な一時値を追加します。
        /// </summary>
        /// <typeparam name="T">追加する値の型。</typeparam>
        /// <param name="value">追加する初期値。</param>
        /// <param name="contextValue">追加された値オブジェクト。</param>
        /// <returns>現在のコンテキストグループ。</returns>
        public FlowContextGroup Add<T>(T value, out FlowContextValue<T> contextValue)
        {
            contextValue = new FlowContextValue<T>(value);
            immutableValues.Insert(0, contextValue);
            values.Insert(0, contextValue);
            return this;
        }

        /// <summary>
        /// 追加済みの一時値をこのコンテキストグループから取り除きます。
        /// </summary>
        /// <typeparam name="T">取り除く値として扱う型。</typeparam>
        /// <param name="contextValue">取り除く値オブジェクト。</param>
        public void Remove<T>(IFlowContextValue contextValue)
        {
            immutableValues.Remove(contextValue);
            values.Remove(contextValue);
        }

        /// <summary>
        /// 追加された一時値を新しい順に探索し、見つからない場合は元のコンテキストから値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。取得できない場合は既定値。</param>
        /// <returns>値を取得できた場合は <see langword="true"/>、取得できない場合は <see langword="false"/>。</returns>
        public bool TryGet<T>(out T? value)
        {
            foreach (var item in immutableValues)
            {
                if (item.TryGet(out value))
                {
                    return true;
                }
            }

            if (mainContext.TryGet(out value))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新可能な一時値を新しい順に探索して更新し、見つからない場合は元のコンテキストへ設定を委譲します。
        /// </summary>
        /// <typeparam name="T">設定する値の型。</typeparam>
        /// <param name="value">設定する値。</param>
        /// <returns>値を設定できた場合は <see langword="true"/>、設定できない場合は <see langword="false"/>。</returns>
        public bool TrySet<T>(T? value)
        {
            foreach (var item in values)
            {
                if (item.TrySet(value))
                {
                    return true;
                }
            }

            if (mainContext.TrySet(value))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新可能な一時値を新しい順に探索し、関数で生成した値を設定します。
        /// </summary>
        /// <typeparam name="T">設定する値の型。</typeparam>
        /// <param name="select">設定する値を生成する関数。</param>
        /// <returns>値を設定できた場合は <see langword="true"/>、設定できない場合は <see langword="false"/>。</returns>
        public bool TrySet<T>(Func<T> select)
        {
            foreach (var item in values)
            {
                if (item.TrySet(select))
                {
                    return true;
                }
            }

            if (mainContext.TrySet(select))
            {
                return true;
            }

            return false;
        }
    }
}
