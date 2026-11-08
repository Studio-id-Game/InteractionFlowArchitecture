using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Entities.Contexts;

public interface ICounterInputAccess
{
    ValueTask<CounterCommand> ReadCommandAsync(CancellationToken token);

    bool TrySubmit(CounterCommand command);
}
