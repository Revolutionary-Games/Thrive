namespace Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Simulates running <see cref="SystemToSchedule"/> accross multiple threads and generates barriers for
    ///   correctness
    /// </summary>
    public class ThreadedRunSimulator
    {
        private readonly List<Thread> threads = new();

        public ThreadedRunSimulator(IEnumerable<IReadOnlyList<SystemToSchedule>> threadTasks)
        {
            int counter = 1;
            foreach (var thread in threadTasks)
            {
                threads.Add(new Thread(thread, counter++));
            }
        }

        public ThreadedRunSimulator(params IReadOnlyList<SystemToSchedule>[] threadTasks) :
            this(threadTasks.AsEnumerable())
        {
        }

        public void Simulate()
        {
            // Setup simulation variables
            foreach (var thread in threads)
            {
                thread.Start();
            }

            var currentTimeslot = new Timeslot(1);

            int deadlockCounter = 0;

            var doneThreads = new HashSet<Thread>();

            // Create time steps until all threads are done
            while (true)
            {
                bool threadsActive = false;
                bool systemsActive = false;

                ++deadlockCounter;
                doneThreads.Clear();

                foreach (var thread in threads)
                {
                    if (thread.Done)
                    {
                        doneThreads.Add(thread);
                        continue;
                    }

                    threadsActive = true;

                    if (currentTimeslot.CanRunSystemInParallel(thread.RunningSystem, thread))
                    {
                        currentTimeslot.MarkConcurrentlyRunningSystem(thread.RunningSystem, thread);
                        deadlockCounter = 0;
                        systemsActive = true;
                    }
                    else
                    {
                        currentTimeslot.MarkThreadWaiting(thread);
                    }
                }

                if (!threadsActive)
                {
                    break;
                }

                if (deadlockCounter > 1000)
                {
                    throw new Exception("Simulated threads cannot progress, likely deadlocked");
                }

                if (!systemsActive)
                {
                    // Time to move to a new timeslot
                    currentTimeslot = currentTimeslot.StartNextTimeslot(doneThreads);
                }
            }

            RemoveUnnecessaryDoubleBarriers();
        }

        private void RemoveUnnecessaryDoubleBarriers()
        {
            // Reset thread points to the start
            foreach (var thread in threads)
            {
                thread.Start();
            }

            while (true)
            {
                bool doubleBarrierMissing = false;
                bool hasDoubleBarriers = false;
                bool completed = true;

                foreach (var thread in threads)
                {
                    if (thread.Done)
                    {
                        // TODO: should finished threads not be taken into account and allowing other threads to remove
                        // double barriers
                        doubleBarrierMissing = true;
                        continue;
                    }

                    completed = false;

                    if (thread.ScanForNextDoubleBarrier())
                    {
                        hasDoubleBarriers = true;
                    }
                    else
                    {
                        doubleBarrierMissing = true;
                    }
                }

                if (hasDoubleBarriers)
                {
                    if (!doubleBarrierMissing)
                    {
                        // Can remove a double barrier
                        foreach (var thread in threads)
                        {
                            thread.RemoveDoubleBarrier();
                        }
                    }
                    else
                    {
                        // Only some threads could remove a barrier, step threads forward to skip this location
                        foreach (var thread in threads)
                        {
                            thread.Step();
                        }
                    }
                }
                else if (completed)
                {
                    // No double barriers to remove anymore
                    break;
                }
            }
        }

        /// <summary>
        ///   Represents what's allowed to happen at the same moment in time
        /// </summary>
        private class Timeslot
        {
            // All reads / runs need to be stored per-thread so when one thread is blocked another can still
            private readonly Dictionary<Thread, HashSet<Type>> componentReads = new();
            private readonly Dictionary<Thread, HashSet<Type>> componentWrites = new();
            private readonly Dictionary<Thread, List<SystemToSchedule>> runSystems = new();

            /// <summary>
            ///   Threads that are blocked and only can resume next timeslot
            /// </summary>
            private readonly List<Thread> threadsToResumeNextTimeslot = new();

            private readonly int time;

            private readonly SystemToSchedule.SystemRequirementsBasedComparer comparer = new();

            public Timeslot(int time)
            {
                this.time = time;
            }

            public bool CanRunSystemInParallel(SystemToSchedule systemToSchedule, Thread thread)
            {
                // Check for timing conflicts with *other* threads
                var otherReads = componentReads.Where(p => p.Key != thread).SelectMany(p => p.Value);
                var otherWrites = componentWrites.Where(p => p.Key != thread).SelectMany(p => p.Value);
                var otherSystems = runSystems.Where(p => p.Key != thread).SelectMany(p => p.Value);

                // Read / write conflicts
                if (otherReads.Any(c => systemToSchedule.WritesComponents.Contains(c)))
                    return false;

                if (otherWrites.Any(c =>
                        systemToSchedule.WritesComponents.Contains(c) || systemToSchedule.ReadsComponents.Contains(c)))
                {
                    return false;
                }

                // Conflicts with hard system ordering (should run after any of the other systems already running)
                if (otherSystems.Any(s => comparer.CompareWeak(systemToSchedule, s) > 0))
                    return false;

                // Check that the system is not running before a later system it should come after from a blocked
                // thread
                foreach (var blockedThread in threadsToResumeNextTimeslot)
                {
                    if (blockedThread == thread)
                        continue;

                    foreach (var futureSystem in blockedThread.GetStillUpcomingSystems())
                    {
                        if (comparer.CompareWeak(systemToSchedule, futureSystem) > 0)
                        {
                            // Need to wait for a blocked thread to resume and schedule a system from it before running
                            return false;
                        }
                    }
                }

                // TODO: should this also check for other future systems from existing threads?
                // It's probably unnecessary as long as the system general list is sorted fully before being split into
                // thread tasks

                // No conflicts with other things happening in this timeslot
                return true;
            }

            public void MarkThreadWaiting(Thread thread)
            {
                if (threadsToResumeNextTimeslot.Contains(thread))
                    return;

                threadsToResumeNextTimeslot.Add(thread);

                var system = thread.RunningSystem;

                ++system.RequiresBarrierBefore;
            }

            public void MarkConcurrentlyRunningSystem(SystemToSchedule systemToSchedule, Thread thread)
            {
                if (systemToSchedule != thread.RunningSystem)
                    throw new ArgumentException("not currently running in thread", nameof(systemToSchedule));

                // Blocked threads cannot run again before the next timeslot
                if (threadsToResumeNextTimeslot.Contains(thread))
                    throw new ArgumentException("Blocked thread cannot resume in this timeslot");

                if (!CanRunSystemInParallel(systemToSchedule, thread))
                    throw new InvalidOperationException("Cannot run the system in parallel");

                if (!runSystems.TryGetValue(thread, out var threadSystems))
                {
                    threadSystems = new List<SystemToSchedule>();
                    runSystems[thread] = threadSystems;
                }

                if (threadSystems.Contains(systemToSchedule))
                    throw new InvalidOperationException("System is already running");

                threadSystems.Add(systemToSchedule);

                // Component writes and reads
                if (!componentReads.TryGetValue(thread, out var threadReads))
                {
                    threadReads = new HashSet<Type>();
                    componentReads[thread] = threadReads;
                }

                foreach (var component in systemToSchedule.ReadsComponents)
                {
                    threadReads.Add(component);
                }

                if (!componentWrites.TryGetValue(thread, out var threadWrites))
                {
                    threadWrites = new HashSet<Type>();
                    componentWrites[thread] = threadWrites;
                }

                foreach (var component in systemToSchedule.WritesComponents)
                {
                    threadWrites.Add(component);
                }

                systemToSchedule.Timeslot = time;

                // Current system from thread is ran, step it to the next system
                thread.Step();
            }

            public Timeslot StartNextTimeslot(IReadOnlyCollection<Thread> doneThreads)
            {
                AddThreadBarrierForUnblockedThreads();

                if (doneThreads.Count > 0)
                    AddBarriersForEarlyExitedThreads(doneThreads);

                var nextSlot = new Timeslot(time + 1);

                foreach (var thread in threadsToResumeNextTimeslot)
                {
                    if (nextSlot.CanRunSystemInParallel(thread.RunningSystem, thread))
                    {
                        nextSlot.MarkConcurrentlyRunningSystem(thread.RunningSystem, thread);
                    }
                    else
                    {
                        // Still need for this thread to wait
                        nextSlot.MarkThreadWaiting(thread);
                    }
                }

                return nextSlot;
            }

            public override string ToString()
            {
                return $"Moment in time: {time}";
            }

            private void AddThreadBarrierForUnblockedThreads()
            {
                foreach (var pair in runSystems)
                {
                    // Skip systems that already got blocked and have a barrier set for this timeslot
                    if (threadsToResumeNextTimeslot.Contains(pair.Key))
                        continue;

                    var system = pair.Value.Last();
                    if (system.RequiresBarrierAfter > 0)
                        throw new Exception("Barrier shouldn't be set already");

                    ++system.RequiresBarrierAfter;
                }
            }

            /// <summary>
            ///   To keep barrier counts in sync, threads that have ran out of work still need to trigger all the
            ///   barriers
            /// </summary>
            /// <param name="doneThreads">Threads that are complete</param>
            private void AddBarriersForEarlyExitedThreads(IReadOnlyCollection<Thread> doneThreads)
            {
                foreach (var doneThread in doneThreads)
                {
                    if (runSystems.ContainsKey(doneThread))
                    {
                        // A thread was able to run a system before completing
                        continue;
                    }

                    if (threadsToResumeNextTimeslot.Contains(doneThread))
                        continue;

                    doneThread.AddDummyBarrierAtEnd();
                }
            }
        }

        private class Thread
        {
            public readonly int ThreadId;

            private readonly IReadOnlyList<SystemToSchedule> threadTasks;

            private int executionIndex = -1;

            public Thread(IReadOnlyList<SystemToSchedule> threadTasks, int threadId)
            {
                if (threadTasks.Count < 1)
                    throw new ArgumentException("Thread must have at least one task");

                this.threadTasks = threadTasks;
                ThreadId = threadId;

                foreach (var systemToSchedule in threadTasks)
                {
                    systemToSchedule.ThreadId = ThreadId;
                }
            }

            public bool Done => executionIndex >= threadTasks.Count;

            public SystemToSchedule RunningSystem
            {
                get
                {
                    if (Done)
                        throw new InvalidOperationException("Already done");

                    return threadTasks[executionIndex];
                }
            }

            public void Start()
            {
                executionIndex = 0;
            }

            public void Step()
            {
                if (Done)
                    return;

                ++executionIndex;
            }

            public IEnumerable<SystemToSchedule> GetStillUpcomingSystems()
            {
                if (Done)
                    yield break;

                for (int i = executionIndex; i < threadTasks.Count; ++i)
                {
                    yield return threadTasks[i];
                }
            }

            public bool ScanForNextDoubleBarrier()
            {
                if (Done)
                    return false;

                // Multiple barriers between systems
                bool afterBarrier = RunningSystem.RequiresBarrierAfter > 0;

                while (true)
                {
                    // Single system having a double barrier
                    if (RunningSystem.RequiresBarrierBefore > 1 || RunningSystem.RequiresBarrierAfter > 1)
                        return true;

                    ++executionIndex;

                    if (Done)
                        break;

                    bool nextBeforeBarrier = RunningSystem.RequiresBarrierBefore > 0;
                    bool nextAfterBarrier = RunningSystem.RequiresBarrierAfter > 0;

                    if (afterBarrier && nextBeforeBarrier)
                    {
                        // A double barrier
                        return true;
                    }

                    afterBarrier = nextAfterBarrier;

                    // If no double barrier was found, stop at the first barrier to not mess up inter-barrier group
                    // ordering
                    if (nextBeforeBarrier || nextAfterBarrier)
                        break;
                }

                return false;
            }

            public void RemoveDoubleBarrier()
            {
                if (Done)
                    throw new InvalidOperationException("Thread is already at the end, not at a system");

                if (RunningSystem.RequiresBarrierBefore > 1)
                {
                    --RunningSystem.RequiresBarrierBefore;

                    // Allow triple barrier detection (don't move if there are still many barriers here)
                    if (RunningSystem.RequiresBarrierBefore < 2)
                        ++executionIndex;
                }
                else if (RunningSystem.RequiresBarrierAfter > 1)
                {
                    --RunningSystem.RequiresBarrierAfter;

                    // Allow triple barrier detection (don't move if there are still many barriers here)
                    if (RunningSystem.RequiresBarrierAfter < 2)
                        ++executionIndex;
                }
                else if (RunningSystem.RequiresBarrierBefore > 0)
                {
                    // Sanity check
                    if (threadTasks[executionIndex - 1].RequiresBarrierAfter < 1)
                    {
                        throw new Exception(
                            "Current point should be detected as double barrier between systems, but it was " +
                            "not found");
                    }

                    --RunningSystem.RequiresBarrierBefore;
                    ++executionIndex;
                }
                else
                {
                    throw new InvalidOperationException("Couldn't find the double barrier to remove");
                }
            }

            public void AddDummyBarrierAtEnd()
            {
                ++threadTasks[threadTasks.Count - 1].RequiresBarrierAfter;
            }

            public override string ToString()
            {
                return $"Thread {ThreadId}";
            }
        }
    }
}
