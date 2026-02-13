using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
///   A simple thread synchronization barrier
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 128)]
public class SimpleBarrier
{
    [FieldOffset(0)]
    private readonly int threadCount;

    // remaining participants on the local phase
    [FieldOffset(4)]
    private volatile int remainingParticipants;

    // local phase
    [FieldOffset(64)]
    private volatile int currentPhase;

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

            CPUHelpers.HyperThreadWake();
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
