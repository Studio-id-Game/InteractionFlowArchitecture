using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Operations;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Operations
{
    public class ConsoleOperation : Operation, IConsoleOperation
    {
        public ConsoleOperation()
        {
            ForceResetMemoryState();
        }

        public override void ForceResetMemoryState()
        {
            State = ConsoleState.Default;

            CancelWaitTime = 100;
        }

        public ConsoleState State { get; set; }

        public int CancelWaitTime { get; set; }

        public ValueTask<ConsoleInputText> WaitUserTextAsync(IFlowContext context)
        {
            return new(CancelableConsoleReadAsync<ConsoleInputText>(context, () =>
            {
                return new(Console.ReadLine());
            }));
        }

        public ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context)
        {
            return WaitUserKeyAsync(context, false);
        }

        public ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context, bool hideChar)
        {
            return new(CancelableConsoleReadAsync<ConsoleInputKeyInfo>(context, () =>
            {
                return new(Console.ReadKey(hideChar));
            }));
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
            var consoleTask = Task.Run(() =>
            {
                return Read(() =>
                {
                    var result = read();
                    keyEnd = true;
                    return result;
                });
            })
            .ContinueWith(async t =>
            {
                // これにより、入力終了+CancelWaitTimeの間待機し、CancelKeyPress -> CancellationTokenSource.Cancel() による条件チェックも正常に働く。
                await Task.Delay(CancelWaitTime, cancellationToken);
                return t.Result;
            })
            .Unwrap();

            var endTask = await Task.WhenAny(cancellationTask, consoleTask);

            if (cancellationToken.IsCancellationRequested)
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

                await consoleTask;
                throw new OperationCanceledException(cancellationToken);
            }
            else
            {
                return await consoleTask;
            }
        }

        private T Read<T>(Func<T> read)
        {
            T result;
            using (GetStateScope())
            {
                Console.BackgroundColor = State.backgroundColor;
                Console.ForegroundColor = State.foregroundColor;

                if (State.writeLine)
                {
                    result = read();
                    Console.WriteLine();
                }
                else
                {
                    result = read();
                }
            }

            Console.BackgroundColor = State.backgroundColor;
            Console.ForegroundColor = State.foregroundColor;

            return result;
        }

        private void Write(string text)
        {
            using (GetStateScope())
            {
                Console.BackgroundColor = State.backgroundColor;
                Console.ForegroundColor = State.foregroundColor;

                if (State.writeLine)
                {
                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(text);
                }
            }

            Console.BackgroundColor = State.backgroundColor;
            Console.ForegroundColor = State.foregroundColor;
        }

        public StateScope<ConsoleOperation, ConsoleState> GetStateScope()
        {
            return State.GetScope(this, (e, value) => e.State = value);
        }

        public class Dummy : Operation, IConsoleOperation.IDummy
        {
            public Dummy()
            {
                ForceResetMemoryState();
            }

            public override void ForceResetMemoryState()
            {
                State = ConsoleState.Default;

                CancelWaitTime = 100;

                DummyText = new("Dummy Text");

                DummyKeyInfo = new(new('A', ConsoleKey.A, true, false, false));

                InputDelayTime = 250;
            }

            public ConsoleInputText DummyText { get; set; }

            public ConsoleInputKeyInfo DummyKeyInfo { get; set; }

            public int InputDelayTime { get; set; }

            public ConsoleState State { get; set; }

            public int CancelWaitTime { get; set; }


            public async ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context)
            {
                await Task.Delay(InputDelayTime);
                Console.WriteLine("<AutoKeyInfo>" + DummyKeyInfo.key);
                return DummyKeyInfo;
            }

            public ValueTask<ConsoleInputKeyInfo> WaitUserText(IFlowContext context)
            {
                return WaitUserKeyAsync(context, false);

            }

            public async ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context, bool hideChar)
            {
                await Task.Delay(InputDelayTime);
                if (!hideChar)
                {
                    Console.WriteLine("<AutoKeyInfo>" + DummyKeyInfo.key);
                }
                return DummyKeyInfo;
            }

            public async ValueTask<ConsoleInputText> WaitUserTextAsync(IFlowContext context)
            {
                await Task.Delay(InputDelayTime);
                Console.WriteLine("<AutoText>" + DummyText.text);
                return DummyText;
            }
        }
    }
}
