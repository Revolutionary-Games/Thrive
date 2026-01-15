using System;
using System.Threading;

/// <summary>
///   A simple thread synchronization barrier
/// </summary>
public class SimpleBarrier
{
    private readonly int threadCount;

    // local phase
    private volatile int currentPhase = 0;

    // remaining participants on the local phase
    private volatile int remainingParticipants;

    public SimpleBarrier(int count)
    {
        threadCount = count;
        remainingParticipants = count;
    }

    public void SignalAndWait()
    {
        int phase = currentPhase;

        int remaining = Interlocked.Decrement(ref remainingParticipants);

        if (remaining == 0)
        {
            // we're the manager thread
            remainingParticipants = threadCount;

            // phase change
            Interlocked.Increment(ref currentPhase);
        }
        else
        {
            while (currentPhase == phase)
            {
                CPUHelpers.HyperThreadPause();
            }
        }
    }
}
