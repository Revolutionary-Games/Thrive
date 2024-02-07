using System;
using System.Threading;

/// <summary>
///   A simple thread synchronization barrier
/// </summary>
public class SimpleBarrier
{
    private const int BUSY_LOOP_COUNT = 100;
    private const int READ_TESTS_IN_A_ROW = 5;

    private readonly int threadCount;
    private readonly Action<SimpleBarrier>? onBarrierStepComplete;

    private int waitingThreads;

    private int releasedThreads;

    /// <summary>
    ///   Setup a new barrier with a given thread count
    /// </summary>
    /// <param name="threadCount">
    ///   How many threads participate in this barrier. If different number of threads are used this will go extremely
    ///   wrong.
    /// </param>
    /// <param name="onBarrierStepComplete">
    ///   Callback to call when all threads have arrived. Note that this is not thread safe, some threads will already
    ///   have been released from the barrier when the callback is invoked.
    /// </param>
    public SimpleBarrier(int threadCount, Action<SimpleBarrier>? onBarrierStepComplete = null)
    {
        if (threadCount < 1)
            throw new ArgumentException("Threads must be at least 1", nameof(this.threadCount));

        this.threadCount = threadCount;
        this.onBarrierStepComplete = onBarrierStepComplete;
    }

    public void SignalAndWait()
    {
        // New thread arriving at the barrier, increment the count
        int readCount = Interlocked.Increment(ref waitingThreads);

        bool managerThread;

        if (readCount == threadCount)
        {
            // We are the last thread to arrive, we need to handle the cleanup of this barrier cycle
            managerThread = true;
        }
        else
        {
            managerThread = false;

            // Wait until all threads have arrived
            while (readCount != threadCount)
            {
                for (int i = 0; i < READ_TESTS_IN_A_ROW; ++i)
                {
                    readCount = waitingThreads;

                    if (readCount == threadCount)
                        break;
                }

                if (readCount == threadCount)
                    break;

                // Try to reduce contention on the atomic variable a bit
                for (int i = 0; i < BUSY_LOOP_COUNT; ++i)
                {
                    _ = i;
                }
            }
        }

        // Threads have all arrived, and are being released

        // Ensure that after the barrier all thread writes and reads are seen by all threads
        Interlocked.MemoryBarrier();

        // Use another atomic variable to control state reset on thread release
        readCount = Interlocked.Increment(ref releasedThreads);

        // The last thread to arrive is the one that needs to clean up the waiting state
        if (managerThread)
        {
            // Wait until all threads are leaving the loop making it safe to modify the wait count without trapping
            // any threads in the wait above
            while (readCount != threadCount)
            {
                readCount = releasedThreads;
            }

            // Add is used here to ensure no problems occur if another thread has already arrived at this barrier again
            Interlocked.Add(ref waitingThreads, -threadCount);

            // Reset the release variable state as well for the next barrier cycle
            if (Interlocked.CompareExchange(ref releasedThreads, 0, threadCount) != threadCount)
            {
                throw new Exception("Barrier released thread count was unexpected");
            }

            Interlocked.MemoryBarrier();

            // Last thread to arrive calls the barrier step complete callback. Note that other threads are already
            // running free
            onBarrierStepComplete?.Invoke(this);
        }
    }
}
