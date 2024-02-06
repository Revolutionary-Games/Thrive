namespace Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Simulates running <see cref="SystemToSchedule"/> accross multiple allThreads and generates barriers for
    ///   correctness
    /// </summary>
    public class ThreadedRunSimulator
    {
        private const float AheadPenaltyPerTask = 1.0f;
        private const float DisallowTasksAfterPenalty = 1.5f;

        private readonly IReadOnlyCollection<SystemToSchedule> freelyAssignableTasks;
        private readonly List<Thread> threads = new();
        private bool simulated;

        public ThreadedRunSimulator(IReadOnlyCollection<SystemToSchedule> mainThreadTasks,
            IReadOnlyCollection<SystemToSchedule> freelyAssignableTasks, int threadCount)
        {
            if (threadCount < 1)
                throw new ArgumentException("Must have at least one thread");

            this.freelyAssignableTasks = freelyAssignableTasks;

            for (int i = 0; i < threadCount; ++i)
            {
                var thread = new Thread(i + 1);
                threads.Add(thread);

                if (i == 0)
                {
                    // Main tasks are reserved for first thread
                    thread.AddExclusiveTasks(mainThreadTasks);
                }
            }
        }

        /// <summary>
        ///   Simulate the allThreads and return list of tasks for each thread
        /// </summary>
        /// <returns>The tasks, first item is always the main thread tasks</returns>
        public List<List<SystemToSchedule>> Simulate()
        {
            // Can't be called again as internal state of the thread objects would need to be cleared
            if (simulated)
                throw new InvalidOperationException("This method cannot be called again");

            simulated = true;

            var currentTimeslot = new Timeslot(1, threads, freelyAssignableTasks);

            int deadlockCounter = 0;

            // Create time steps until all allThreads are done
            while (currentTimeslot.HasUpcomingTasks())
            {
                bool systemsActive = false;

                ++deadlockCounter;

                foreach (var thread in threads)
                {
                    if (currentTimeslot.ScheduleWorkForThread(thread))
                    {
                        deadlockCounter = 0;
                        systemsActive = true;
                    }
                }

                if (deadlockCounter > 1000)
                {
                    throw new Exception("Simulated allThreads cannot progress, likely deadlocked");
                }

                if (!systemsActive)
                {
                    // Time to move to a new timeslot
                    currentTimeslot = currentTimeslot.StartNextTimeslot();
                }
            }

            var result = new List<List<SystemToSchedule>>();

            foreach (var thread in threads)
            {
                if (thread.GetStillUpcomingSystems().Any())
                    throw new Exception("A thread still has upcoming tasks");

                thread.ApplyThreadResultsToSystems();

                result.Add(thread.GetAllExecutedTasks());
            }

            return result;
        }

        /// <summary>
        ///   Represents what's allowed to happen at the same moment in time
        /// </summary>
        private class Timeslot
        {
            /// <summary>
            ///   Tasks that have not been executed yet and still need to be attempted to be scheduled on the allThreads
            /// </summary>
            private readonly List<SystemToSchedule> upcomingTasks;

            private readonly IReadOnlyList<Thread> allThreads;

            private readonly int time;

            // All reads / runs need to be stored per-thread so when one thread is blocked another can still
            private readonly Dictionary<Thread, HashSet<Type>> componentReads = new();
            private readonly Dictionary<Thread, HashSet<Type>> componentWrites = new();
            private readonly Dictionary<Thread, List<SystemToSchedule>> runSystems = new();

            private readonly SystemToSchedule.SystemRequirementsBasedComparer comparer = new();

            public Timeslot(int time, IReadOnlyList<Thread> threads, IEnumerable<SystemToSchedule> upcomingTasks)
            {
                this.time = time;
                allThreads = threads;
                this.upcomingTasks = upcomingTasks.ToList();
            }

            public bool ScheduleWorkForThread(Thread thread)
            {
                // If thread is too much ahead, cannot schedule more work
                if (thread.AheadPenalty >= DisallowTasksAfterPenalty)
                    return false;

                // Prioritize running exclusive tasks
                var exclusiveTask = thread.NextExclusiveTask;

                if (exclusiveTask != null)
                {
                    if (CanRunSystemInParallel(exclusiveTask, thread))
                    {
                        MarkConcurrentlyRunningSystem(exclusiveTask, thread);
                        return true;
                    }
                }

                // No good exclusive task, try from general task pool
                foreach (var task in upcomingTasks)
                {
                    if (CanRunSystemInParallel(task, thread))
                    {
                        MarkConcurrentlyRunningSystem(task, thread);
                        return true;
                    }
                }

                // Thread cannot start work
                MarkThreadWaiting(thread);
                return false;
            }

            public bool HasUpcomingTasks()
            {
                return upcomingTasks.Count > 0 || allThreads.Any(t => t.NextExclusiveTask != null);
            }

            public Timeslot StartNextTimeslot()
            {
                // Add barrier between slots if more than 1 thread executed in this timeslot
                bool addBarrier = runSystems.Count(p => p.Value.Count > 0) > 1;

                // Reset thread timeslot-specific values
                foreach (var thread in allThreads)
                {
                    thread.OnTimeslotStarted();

                    if (addBarrier)
                    {
                        thread.AddBarrierAtEnd();
                    }
                }

                return new Timeslot(time + 1, allThreads, upcomingTasks);
            }

            public override string ToString()
            {
                return $"Moment in time: {time}";
            }

            private bool CanRunSystemInParallel(SystemToSchedule systemToSchedule, Thread thread)
            {
                // Check for timing conflicts with *other* allThreads
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

                // Check that the system is not running before a later system it should come after from a thread or
                // the upcoming tasks
                foreach (var otherThread in allThreads)
                {
                    if (otherThread == thread)
                        continue;

                    foreach (var futureSystem in otherThread.GetStillUpcomingSystems())
                    {
                        if (comparer.CompareWeak(systemToSchedule, futureSystem) > 0)
                        {
                            // Need to wait for this thread to manage to run this task
                            return false;
                        }
                    }
                }

                foreach (var upcomingTask in upcomingTasks)
                {
                    if (comparer.CompareWeak(systemToSchedule, upcomingTask) > 0)
                    {
                        // Need to wait until some thread takes this upcoming task before the checked task can be run
                        return false;
                    }
                }

                // No conflicts with other things happening in this timeslot
                return true;
            }

            private void MarkThreadWaiting(Thread thread)
            {
                // Add penalty for other threads if this thread cannot progress so that other threads don't add a ton
                // of unbalanced systems
                foreach (var otherThread in allThreads)
                {
                    if (thread == otherThread)
                        continue;

                    otherThread.AheadPenalty += AheadPenaltyPerTask;
                }
            }

            private void MarkConcurrentlyRunningSystem(SystemToSchedule systemToSchedule, Thread thread)
            {
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
                thread.MarkExecutedTask(systemToSchedule);

                upcomingTasks.Remove(systemToSchedule);
            }
        }

        private class Thread
        {
            private readonly int threadId;

            private readonly List<SystemToSchedule> threadTasks = new();
            private readonly List<SystemToSchedule> upcomingExclusiveTasks = new();

            public Thread(int threadId)
            {
                this.threadId = threadId;
            }

            public float AheadPenalty { get; set; }

            public SystemToSchedule? NextExclusiveTask => upcomingExclusiveTasks.FirstOrDefault();

            public IEnumerable<SystemToSchedule> GetStillUpcomingSystems()
            {
                return upcomingExclusiveTasks;
            }

            public void MarkExecutedTask(SystemToSchedule system)
            {
                threadTasks.Add(system);

                upcomingExclusiveTasks.Remove(system);
            }

            public void AddBarrierAtEnd()
            {
                if (threadTasks.Count < 1)
                    return;

                ++threadTasks[threadTasks.Count - 1].RequiresBarrierAfter;
            }

            public void AddExclusiveTasks(IReadOnlyCollection<SystemToSchedule> exclusiveTasks)
            {
                upcomingExclusiveTasks.AddRange(exclusiveTasks);
            }

            public void ApplyThreadResultsToSystems()
            {
                foreach (var systemToSchedule in threadTasks)
                {
                    systemToSchedule.ThreadId = threadId;
                }
            }

            public List<SystemToSchedule> GetAllExecutedTasks()
            {
                if (threadTasks.Count < 1)
                    throw new InvalidOperationException("Thread doesn't have any tasks");

                return threadTasks;
            }

            public override string ToString()
            {
                return $"Thread {threadId}";
            }

            internal void OnTimeslotStarted()
            {
                AheadPenalty = 0;
            }
        }
    }
}
