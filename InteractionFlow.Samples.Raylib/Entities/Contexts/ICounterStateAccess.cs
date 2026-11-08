using System;

namespace InteractionFlow.Samples.Raylib.Entities.Contexts;

public interface ICounterStateAccess
{
    int Count { get; }

    bool ExitRequested { get; }

    bool BeginIncrement();

    bool CompleteIncrement();

    void Cancel();

    void Fail(Exception exception);

    void RequestExit();
}
