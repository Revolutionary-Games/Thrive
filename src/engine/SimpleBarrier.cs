using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
///   A simple thread synchronization barrier
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
public class SimpleBarrier
{
    [FieldOffset(0)]
    private readonly int threadCount;

    // local phase
    [FieldOffset(64)]
    private volatile int currentPhase;

    // remaining participants on the local phase
    [FieldOffset(128)]
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
                // TODO: Use the ARM instruction WFE on ARM.

                CPUHelpers.HyperThreadPause();
            }
        }

        // TODO: Use the ARM instruction SEV on ARM.
    }
}
