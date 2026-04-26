using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Operations;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Operations
{
    public class ConsoleOperation : Operation<ConsoleInputText>, IConsoleOperation
    {
        public class Dummy : IConsoleOperation.IDummy,
            IValueOperation<ConsoleInputAnyKey>,
            IValueOperation<ConsoleInputKeyInfo>,
            IValueOperation<ConsoleInputText>
        {
            private readonly ValueOperation<ConsoleInputAnyKey> valueAnyKey;
            private readonly ValueOperation<ConsoleInputKeyInfo> valueKeyInfo;
            private readonly ValueOperation<ConsoleInputText> valueText;

            public Dummy()
            {
                valueAnyKey = new(async () =>
                {
                    await Task.Delay(DelayTime);
                    Console.WriteLine("<AutoAnyKey>");
                    return new ConsoleInputAnyKey();
                });
                valueKeyInfo = new(async () =>
                {
                    await Task.Delay(DelayTime);
                    Console.WriteLine("<AutoKeyInfo>" + KeyInfo.key);
                    return KeyInfo;
                });

                valueText = new(async () =>
                {
                    await Task.Delay(DelayTime);
                    Console.WriteLine("<AutoText>" + Text.text);
                    return Text;
                });
            }

            public ConsoleInputText Text { get; set; } = new("Dummy Text");

            public ConsoleInputKeyInfo KeyInfo { get; set; } = new(new('A', ConsoleKey.A, true, false, false));

            public int DelayTime { get; set; } = 250;

            public ConsoleState State { get; set; }

            public void ForceResetMemoryState()
            {
            }

            public ValueTask<ConsoleInputAnyKey> UserOperateAnyKeyAsync(IFlowContext context) => valueAnyKey.UserOperateAsync(context);

            public ValueTask<ConsoleInputKeyInfo> UserOperateKeyInfoAsync(IFlowContext context) => valueKeyInfo.UserOperateAsync(context);

            public ValueTask<ConsoleInputText> UserOperateTextAsync(IFlowContext context) => valueText.UserOperateAsync(context);
        }

        public ConsoleState State { get; set; } = ConsoleState.DefaultNoLine;

        public override ValueTask<ConsoleInputText> UserOperateAsync(IFlowContext context)
        {
            return UserOperateTextAsync(context);
        }

        public async ValueTask<ConsoleInputText> UserOperateTextAsync(IFlowContext context)
        {
            return await UserOperateAsync(context, () =>
            {
                return new ConsoleInputText(Console.ReadLine());
            });
        }

        public async ValueTask<ConsoleInputKeyInfo> UserOperateKeyInfoAsync(IFlowContext context)
        {
            return await UserOperateAsync(context, () =>
            {
                return new ConsoleInputKeyInfo(Console.ReadKey());
            });
        }

        public async ValueTask<ConsoleInputAnyKey> UserOperateAnyKeyAsync(IFlowContext context)
        {
            return await UserOperateAsync(context, () =>
            {
                Console.ReadKey();
                return new ConsoleInputAnyKey();
            });
        }

        private async Task<TInput> UserOperateAsync<TInput>(IFlowContext context, Func<TInput> read)
        {
            var cancellationToken = context.CancellationToken;

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
                if (State.writeLine)
                    Console.WriteLine();

                TInput res;
                using (State.Use())
                {
                    res = read();
                    keyEnd = true;
                }
                return res;
            })
            .ContinueWith(async t =>
            {
                // これにより、入力終了+100ms秒の間待機し、CancelKeyPress -> CancellationTokenSource.Cancel() による条件チェックも正常に働く。
                await Task.Delay(100, cancellationToken);
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
                    using (State.Use())
                    {
                        Console.Write("[Cancel requested] Press Enter to abort...");
                    }
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

        public override void ForceResetMemoryState()
        {
            State = ConsoleState.DefaultNoLine;
        }

    }
}