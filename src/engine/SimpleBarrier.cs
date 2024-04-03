using System;
using System.Threading;

/// <summary>
///   A simple thread synchronization barrier
/// </summary>
/// <remarks>
///   <para>
///     WARNING: in Godot 4 this seems to be related to process lock up and is likely not safe to use
///   </para>
/// </remarks>
public class SimpleBarrier
{
    private const int BUSY_LOOP_COUNT = 25;
    private const int READ_TESTS_IN_A_ROW = 5;

    private readonly int threadCount;

    private int waitingThreads;

    private int blockedThreads;
    private int threadsInWaitLoop;

    /// <summary>
    ///   Setup a new barrier with a given thread count
    /// </summary>
    /// <param name="threadCount">
    ///   How many threads participate in this barrier. If different number of threads are used this will go extremely
    ///   wrong.
    /// </param>
    public SimpleBarrier(int threadCount)
    {
        if (threadCount < 1)
            throw new ArgumentException("Threads must be at least 1", nameof(this.threadCount));

        this.threadCount = threadCount;
    }

    public void SignalAndWait()
    {
        // Mark this thread as blocked until released from this method
        Interlocked.Increment(ref blockedThreads);
        Interlocked.Increment(ref threadsInWaitLoop);

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

        // Threads have all arrived and should be leaving the above loop, all threads need to wait until all are
        // ready to leave
        readCount = Interlocked.Decrement(ref threadsInWaitLoop);

        while (readCount != 0)
        {
            readCount = threadsInWaitLoop;
        }

        // Reset the state for the next loop. This is now safe as no thread can still be trying to read the wait
        // count in the first loop of this method.
        if (managerThread)
        {
            if (Interlocked.CompareExchange(ref waitingThreads, 0, threadCount) != threadCount)
            {
                throw new Exception("Barrier wait reset after wait complete failed");
            }

            // TODO: check if it would be a better idea to use this (which might allow using one less atomic variable):
            /*// Add is used here to ensure no problems occur if another thread has already arrived at this barrier again
            Interlocked.Add(ref waitingThreads, -threadCount);*/
        }

        // State is now ready for release so all threads can now be released

        readCount = Interlocked.Decrement(ref blockedThreads);

        // All threads need to wait until all threads have been released to not cause any threads to be left behind
        // and no thread being able to reach this barrier again to mess with waitingThreads variable before it is reset
        while (readCount != 0)
        {
            // All threads should be releasing very fast, so just keep trying to read the variable
            readCount = blockedThreads;
        }

        // Ensure that after the barrier all thread writes and reads are seen by all threads
        Interlocked.MemoryBarrier();
    }
}
