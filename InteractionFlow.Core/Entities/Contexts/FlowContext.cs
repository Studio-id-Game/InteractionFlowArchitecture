using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>文脈値の追加・探索とキャンセル制御を提供するフローコンテキストです。</summary>
    /// <remarks>
    /// 親と追加値の所有権は取得しません。変更・探索と破棄の並行実行はサポートしません。
    /// </remarks>
    public class FlowContext : IFlowContext, IDisposable
    {
        private sealed class Box<T>(T value) : Entry<T>(value), IEntry
        {
            bool IEntry.IsContextBox => true;
        }

        private IFlowContext? parentContext;
        private List<IEntry>? values = [];
        private List<IEntry> Values => values ?? throw new ObjectDisposedException(nameof(FlowContext));

        /// <summary>独自のキャンセル状態を持つルートコンテキストを作成します。</summary>
        public FlowContext()
        {
            Cancellation = new CancellationObject();
        }

        /// <summary>親の文脈値を探索し、キャンセル状態を共有する子コンテキストを作成します。</summary>
        /// <param name="parentContext">値の探索先となる親。</param>
        /// <exception cref="ArgumentNullException">親が null の場合。</exception>
        public FlowContext(IFlowContext parentContext) : this(parentContext, ContextInheritance.All)
        {
        }

        /// <summary>親の値探索とキャンセル状態を、指定した範囲で引き継ぐコンテキストを作成します。</summary>
        /// <param name="parentContext">引き継ぎ元。None の場合も null は指定できません。</param>
        /// <param name="inheritance">親から引き継ぐ範囲。</param>
        /// <remarks>
        /// Values は親への探索を有効にし、値をコピーしません。Cancellation を指定しなければ新規に作成します。
        /// Values のみの場合、Cancellation プロパティは独自ですが、値探索では親の CancellationObject が
        /// 返ることがあります。追加値による上書きを含め、値探索はプロパティと独立しています。
        /// 親とその値、共有する CancellationObject の所有権は取得しません。
        /// </remarks>
        /// <exception cref="ArgumentNullException">親が null の場合。</exception>
        /// <exception cref="ArgumentOutOfRangeException">未定義のフラグが含まれる場合。</exception>
        public FlowContext(IFlowContext parentContext, ContextInheritance inheritance)
        {
            if (parentContext is null) throw new ArgumentNullException(nameof(parentContext));
            if ((inheritance & ~ContextInheritance.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(inheritance), inheritance, "Unknown context inheritance flags.");

            this.parentContext = (inheritance & ContextInheritance.Values) != 0 ? parentContext : null;
            Cancellation = (inheritance & ContextInheritance.Cancellation) != 0
                ? parentContext.Cancellation
                : new CancellationObject();
        }

        /// <summary>キャンセル状態を取得します。構築時に Cancellation の引き継ぎを指定した場合は親と共有します。</summary>
        /// <remarks>破棄後も参照できます。Context の破棄でキャンセル・リセットは行いません。</remarks>
        public CancellationObject Cancellation { get; }

        /// <summary>文脈値を追加します。同じ型を後から追加すると探索時に優先されます。</summary>
        /// <typeparam name="T">追加する値の型。</typeparam>
        /// <param name="value">追加する非 null の値。</param>
        /// <returns>現在のコンテキスト。</returns>
        /// <exception cref="ArgumentNullException">値が null の場合。</exception>
        /// <exception cref="ObjectDisposedException">破棄済みの場合。</exception>
        public FlowContext With<T>(T value)
        {
            var entries = Values;
            if (value is null) throw new ArgumentNullException(nameof(value));
            ValidateAdditionalValue(value);
            entries.Add(new Box<T>(value));
            return this;
        }

        /// <summary>このコンテキストへ登録する追加値を検証する、派生型向けの拡張箇所です。</summary>
        /// <typeparam name="T">With に指定された追加値の型。</typeparam>
        /// <param name="value">登録前の非 null の追加値。</param>
        /// <remarks>
        /// 破棄済み・null の確認後、登録前に呼びます。既定では何も拒否しません。
        /// 例外はそのまま伝播し、With は対象値を登録しません。
        /// 検証だけを行い、登録状態を変更しないでください。親コンテキストの検証は呼びません。
        /// </remarks>
        protected virtual void ValidateAdditionalValue<T>(T value) { }

        /// <summary>追加値を新しい順に探索し、その後で派生型の取得処理・親へ委譲します。</summary>
        /// <typeparam name="T">要求する値の型。</typeparam>
        /// <param name="value">取得した値。不在の場合は既定値。</param>
        /// <returns>値を取得できた場合は true。</returns>
        /// <remarks>Entry の未発見以外の解決失敗は、その例外を伝播します。</remarks>
        /// <exception cref="ObjectDisposedException">破棄済みの場合。</exception>
        /// <exception cref="ResultException">Entry の解決に失敗した場合。</exception>
        public bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            var entries = Values;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].TryResolve<T>(false, out var found, out var error))
                {
                    value = found;
                    return true;
                }
                if (error != null) throw new ResultException(error);
            }
            return TryGetCore(out value);
        }

        /// <summary>追加値で見つからなかった値を提供する、派生型向けの拡張箇所です。</summary>
        /// <typeparam name="T">要求する値の型。</typeparam>
        /// <param name="value">取得した値。不在の場合は既定値。</param>
        /// <returns>値を取得できた場合は true。</returns>
        /// <remarks>
        /// Values を引き継いだ場合は親を探索し、そうでなければ自身の Cancellation を提供します。
        /// 派生型は独自値を見つけられなければ base.TryGetCore を呼んでください。
        /// 公開の TryGet が、追加値の優先と破棄チェックを先に行います。
        /// </remarks>
        protected virtual bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
        {
            if (parentContext != null) return parentContext.TryGet(out value);
            if (Cancellation is T cancellation)
            {
                value = cancellation;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>保持する値・親への探索参照を解放し、追加と探索を禁止します。</summary>
        /// <remarks>親・追加値・Cancellation は破棄しません。繰り返し呼び出しても何もしません。</remarks>
        public void Dispose()
        {
            var entries = values;
            if (entries is null) return;
            values = null;
            parentContext = null;
            entries.Clear();
            DisposeCore();
        }

        /// <summary>派生型が自身で所有する資源を解放する拡張箇所です。</summary>
        /// <remarks>Context を破棄済みにした後、一度だけ呼びます。例外は呼び出し側へ伝播します。</remarks>
        protected virtual void DisposeCore() { }
    }
}
