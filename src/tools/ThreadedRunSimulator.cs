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

            // Create time steps until all threads are done
            while (true)
            {
                bool threadsActive = false;
                bool systemsActive = false;

                ++deadlockCounter;

                foreach (var thread in threads)
                {
                    if (thread.Done)
                        continue;

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
                    currentTimeslot = currentTimeslot.StartNextTimeslot();
                }
            }
        }

        /// <summary>
        ///   Represents what's allowed to happen at the same moment in time
        /// </summary>
        private class Timeslot
        {
            // All reads / runs need to be stored per-thread so when one thread is blocked another can still
            public readonly Dictionary<Thread, HashSet<Type>> ComponentReads = new();
            public readonly Dictionary<Thread, HashSet<Type>> ComponentWrites = new();
            public readonly Dictionary<Thread, List<SystemToSchedule>> RunSystems = new();

            /// <summary>
            ///   Threads that are blocked and only can resume next timeslot
            /// </summary>
            public readonly List<Thread> ThreadsToResumeNextTimeslot = new();

            public readonly int Time;

            private readonly SystemToSchedule.SystemRequirementsBasedComparer comparer = new();

            public Timeslot(int time)
            {
                Time = time;
            }

            public bool CanRunSystemInParallel(SystemToSchedule systemToSchedule, Thread thread)
            {
                // Check for timing conflicts with *other* threads
                var otherReads = ComponentReads.Where(p => p.Key != thread).SelectMany(p => p.Value);
                var otherWrites = ComponentWrites.Where(p => p.Key != thread).SelectMany(p => p.Value);
                var otherSystems = RunSystems.Where(p => p.Key != thread).SelectMany(p => p.Value);

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
                foreach (var blockedThread in ThreadsToResumeNextTimeslot)
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
                if (ThreadsToResumeNextTimeslot.Contains(thread))
                    return;

                ThreadsToResumeNextTimeslot.Add(thread);

                var system = thread.RunningSystem;

                ++system.RequiresBarrierBefore;
            }

            public void MarkConcurrentlyRunningSystem(SystemToSchedule systemToSchedule, Thread thread)
            {
                if (systemToSchedule != thread.RunningSystem)
                    throw new ArgumentException("not currently running in thread", nameof(systemToSchedule));

                // Blocked threads cannot run again before the next timeslot
                if (ThreadsToResumeNextTimeslot.Contains(thread))
                    throw new ArgumentException("Blocked thread cannot resume in this timeslot");

                if (!CanRunSystemInParallel(systemToSchedule, thread))
                    throw new InvalidOperationException("Cannot run the system in parallel");

                if (!RunSystems.TryGetValue(thread, out var threadSystems))
                {
                    threadSystems = new List<SystemToSchedule>();
                    RunSystems[thread] = threadSystems;
                }

                if (threadSystems.Contains(systemToSchedule))
                    throw new InvalidOperationException("System is already running");

                threadSystems.Add(systemToSchedule);

                // Component writes and reads
                if (!ComponentReads.TryGetValue(thread, out var threadReads))
                {
                    threadReads = new HashSet<Type>();
                    ComponentReads[thread] = threadReads;
                }

                foreach (var component in systemToSchedule.ReadsComponents)
                {
                    threadReads.Add(component);
                }

                if (!ComponentWrites.TryGetValue(thread, out var threadWrites))
                {
                    threadWrites = new HashSet<Type>();
                    ComponentWrites[thread] = threadWrites;
                }

                foreach (var component in systemToSchedule.WritesComponents)
                {
                    threadWrites.Add(component);
                }

                // Current system from thread is ran, step it to the next system
                thread.Step();
            }

            public Timeslot StartNextTimeslot()
            {
                AddThreadBarrierForUnblockedThreads();

                var nextSlot = new Timeslot(Time + 1);

                foreach (var thread in ThreadsToResumeNextTimeslot)
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
                return $"Moment in time: {Time}";
            }

            private void AddThreadBarrierForUnblockedThreads()
            {
                foreach (var pair in RunSystems)
                {
                    // Skip systems that already got blocked and have a barrier set for this timeslot
                    // This basically means this method doesn't do much as all timeslots are ran until all threads are
                    // blocked. See instead MarkThreadWaiting.
                    if (ThreadsToResumeNextTimeslot.Contains(pair.Key))
                        continue;

                    var system = pair.Value.Last();
                    if (system.RequiresBarrierAfter > 0)
                        throw new Exception("Barrier shouldn't be set already");

                    ++system.RequiresBarrierAfter;
                }
            }
        }

        private class Thread
        {
            public readonly int ThreadId;

            private readonly IReadOnlyList<SystemToSchedule> threadTasks;
            private readonly SystemToSchedule.SystemRequirementsBasedComparer comparer = new();

            private int executionIndex = -1;

            public Thread(IReadOnlyList<SystemToSchedule> threadTasks, int threadId)
            {
                if (threadTasks.Count < 1)
                    throw new ArgumentException("Thread must have at least one task");

                this.threadTasks = threadTasks;
                ThreadId = threadId;
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

            public override string ToString()
            {
                return $"Thread {ThreadId}";
            }
        }
    }
}
