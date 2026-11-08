using InteractionFlow.Core.Entities.Contexts;
using System;

namespace InteractionFlow.Standard.Entities.Contexts
{
    /// <summary>CancellationObject にスコープ付きの時間制限を追加します。</summary>
    public static class CancellationSchedulerExtensions
    {
        /// <summary>指定した対象へキャンセルを通知する Scheduler を取得します。</summary>
        /// <remarks>対象の所有権は取得せず、破棄やリセットもしません。</remarks>
        public static CancellationScheduler GetScheduler(this CancellationObject cancellation)
        {
            if (cancellation is null) throw new ArgumentNullException(nameof(cancellation));
            return new CancellationScheduler(cancellation);
        }
    }
}
