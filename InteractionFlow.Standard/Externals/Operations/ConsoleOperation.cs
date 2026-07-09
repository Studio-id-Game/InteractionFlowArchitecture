using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Operations;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Operations
{
    /// <summary>
    /// 標準入力から文字列またはキーを取得するコンソール Operation 実装です。
    /// </summary>
    public class ConsoleOperation : Operation, IConsoleOperation
    {
        /// <summary>
        /// 既定のコンソール入力状態でインスタンスを作成します。
        /// </summary>
        public ConsoleOperation() : base()
        {
            if (State == null)
                throw new ArgumentNullException("state");
        }

        /// <summary>
        /// コンソール入力状態を既定値へ戻します。
        /// </summary>
        public override void ForceResetMemoryState()
        {
            State = ConsoleOperationState.Default;
        }

        /// <summary>
        /// コンソール入力に使用する状態を取得または設定します。
        /// </summary>
        public ConsoleOperationState State { get; set; }

        /// <summary>
        /// 標準入力から 1 行の文字列を取得します。
        /// </summary>
        /// <param name="context">入力操作に使用するフローコンテキスト。</param>
        /// <returns>入力された文字列。</returns>
        public async ValueTask<ConsoleInputText> WaitUserTextAsync(IFlowContext context)
        {
            return await CancelableConsoleReadAsync<ConsoleInputText>(context, () =>
            {
                return new(Console.ReadLine());
            });
        }

        /// <summary>
        /// 標準入力からキーを取得します。
        /// </summary>
        /// <param name="context">入力操作に使用するフローコンテキスト。</param>
        /// <returns>入力されたキー情報。</returns>
        public ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context)
        {
            return WaitUserKeyAsync(context, false);
        }

        /// <summary>
        /// 標準入力からキーを、表示有無を指定して取得します。
        /// </summary>
        /// <param name="context">入力操作に使用するフローコンテキスト。</param>
        /// <param name="hideChar">入力文字を表示しない場合は <see langword="true"/>。</param>
        /// <returns>入力されたキー情報。</returns>
        public async ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context, bool hideChar)
        {
            return await CancelableConsoleReadAsync<ConsoleInputKeyInfo>(context, () =>
            {
                return new(Console.ReadKey(hideChar));
            });
        }

        private async Task<TInput> CancelableConsoleReadAsync<TInput>(IFlowContext context, Func<TInput> read)
        {
            var cancellationToken = context.Cancellation.GetToken();

            /* # Cancel処理について
            // ConsoleのCancel処理は以下の順に行われる。
            //
            // 1.Ctrl + C 処理
            // 2.ConsoleのTaskが終了する
            // 3.CancelKeyPress イベント処理
            //
            // この時、2. と 3. の間にはタイムラグが存在する。
            // ゆえに、Console.Read~ の後に Delay(100)をいれて、以下の順にする
            //
            // 1.Ctrl + C 処理
            // 2.CancelKeyPress イベント処理
            // 3.ConsoleのTaskが終了する
            // これにより、cancellationToken.IsCancellationRequested による条件チェックが正常に働く。
            //
            // またこの動作は、
            // CancelKeyPress だけ登録されている場合は、空文字判定で正常終了
            // cancellationToken だけ登録されている場合は、ユーザー入力の終了を待機して正常終了する。
            //
            // キャンセルについての仕様：
            // 1. コンソール入力の開始から入力終了後100msの間、
            //    CancellationTokenSource によるキャンセルを受け付ける。
            //    この期間内にキャンセルされた場合、OperationCanceledException を送出する。
            //
            // 2. Ctrl+C による中断は常に検知されるが、
            //    1 の期間外で発生した場合はキャンセルとは扱わず、空入力として処理される。
            //    （遅延したキャンセルは次の操作で処理される）
            */

            bool keyEnd = false;
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            var consoleTask = Task.Run(async () =>
            {
                var result = Read(() =>
                {
                    var result = read();
                    keyEnd = true;
                    return result;
                });

                // これにより、入力終了+CancelWaitTimeの間待機し、CancelKeyPress -> CancellationTokenSource.Cancel() による条件チェックも正常に働く。
                await Task.Delay(State.cancelWaitTime, cancellationToken);
                return result;
            });

            var endTask = await Task.WhenAny(cancellationTask, consoleTask);

            if (endTask.IsCanceled)
            {
                // consoleTaskが終了していない場合は、ユーザー入力を待機して正常終了する。
                if (!keyEnd)
                {
                    Console.WriteLine();
                    Write("[Cancel requested] Press Enter to abort...");
                    Console.WriteLine();
                }

                while (Console.KeyAvailable)
                    Console.ReadKey(true);

            }

            return await consoleTask;
        }

        private T Read<T>(Func<T> read)
        {
            using var cc = new ConsoleColorScope().GetStateScope();
            cc.State = State.ColorSet;

            var readResult = read();
            if (State.writeLine)
            {
                Console.WriteLine();
            }

            return readResult;
        }

        private void Write(string text)
        {
            using var cc = new ConsoleColorScope().GetStateScope();
            cc.State = State.ColorSet;
            if (State.writeLine)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }
        }

        /// <summary>
        /// 実際のコンソール入力を待たず、設定されたダミー値を返す Operation 実装です。
        /// </summary>
        public class Dummy : Operation, IConsoleOperation.IDummy
        {
            /// <summary>
            /// 既定のダミー入力状態でインスタンスを作成します。
            /// </summary>
            public Dummy() : base()
            {
                if (State == null)
                    throw new ArgumentNullException("state");
            }

            /// <summary>
            /// ダミー入力状態と返却値を既定値へ戻します。
            /// </summary>
            public override void ForceResetMemoryState()
            {
                State = ConsoleOperationState.Default;

                DummyText = new("Dummy Text");

                DummyKeyInfo = new(new('A', ConsoleKey.A, true, false, false));

                InputDelayTime = 250;
            }

            /// <summary>
            /// ダミーの文字列入力を取得または設定します。
            /// </summary>
            public ConsoleInputText DummyText { get; set; }

            /// <summary>
            /// ダミーのキー入力を取得または設定します。
            /// </summary>
            public ConsoleInputKeyInfo DummyKeyInfo { get; set; }

            /// <summary>
            /// ダミー入力を返すまでの待機時間を取得または設定します。
            /// </summary>
            public int InputDelayTime { get; set; }

            /// <summary>
            /// ダミー入力表示に使用する状態を取得または設定します。
            /// </summary>
            public ConsoleOperationState State { get; set; }

            /// <summary>
            /// 設定されたダミーキー入力を返します。
            /// </summary>
            /// <param name="context">入力操作に使用するフローコンテキスト。</param>
            /// <returns>ダミーのキー情報。</returns>
            public async ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context)
            {
                await Task.Delay(InputDelayTime);
                Write("<AutoKeyInfo>" + DummyKeyInfo.key);
                return DummyKeyInfo;
            }

            /// <summary>
            /// 設定されたダミーキー入力を、表示有無を指定して返します。
            /// </summary>
            /// <param name="context">入力操作に使用するフローコンテキスト。</param>
            /// <param name="hideChar">ダミーキーを表示しない場合は <see langword="true"/>。</param>
            /// <returns>ダミーのキー情報。</returns>
            public async ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context, bool hideChar)
            {
                await Task.Delay(InputDelayTime);
                if (!hideChar)
                {
                    Write("<AutoKeyInfo>" + DummyKeyInfo.key);
                }
                return DummyKeyInfo;
            }

            /// <summary>
            /// 設定されたダミー文字列入力を返します。
            /// </summary>
            /// <param name="context">入力操作に使用するフローコンテキスト。</param>
            /// <returns>ダミーの文字列入力。</returns>
            public async ValueTask<ConsoleInputText> WaitUserTextAsync(IFlowContext context)
            {
                await Task.Delay(InputDelayTime);
                Write("<AutoText>" + DummyText.text);
                return DummyText;
            }

            private void Write(string text)
            {
                using var cc = new ConsoleColorScope().GetStateScope();
                cc.State = State.ColorSet;
                if (State.writeLine)
                {
                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(text);
                }
            }
        }
    }
}
