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

            private Timeslot? previousSlot;

            public Timeslot(int time, IReadOnlyList<Thread> threads, IEnumerable<SystemToSchedule> upcomingTasks)
            {
                this.time = time;
                allThreads = threads;
                this.upcomingTasks = upcomingTasks.ToList();
            }

            private int RunThreadsCount => runSystems.Count(p => p.Value.Count > 0);

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
                // // Add barrier between slots if more than 1 thread executed in this timeslot
                int runThreadCount = RunThreadsCount;

                if (runThreadCount < 1)
                    throw new Exception("Ran threads in slot count is 0");

                // bool addBarrier = runThreadCount > 1;
                bool addBarrier = true;

                // Reset thread timeslot-specific values
                foreach (var thread in allThreads)
                {
                    thread.OnTimeslotStarted();

                    if (addBarrier)
                    {
                        thread.AddBarrierAtEnd();
                    }
                }

                int timeOffsetForNextSlot = 1;

                var previous = this;

                // Combine two consequent slots where only one thread could run
                if (runThreadCount == 1 && previousSlot is { RunThreadsCount: 1 })
                {
                    // TODO: broken code
                    /*// Next slot will run with the same time as this one as this slot didn't "happen"
                    timeOffsetForNextSlot = 0;
                    OnCombineWithPrevious();

                    if (previousSlot.RunThreadsCount != 1)
                        throw new Exception("Combine caused run threads count to change");

                    previous = previousSlot;*/
                }

                var nextSlot = new Timeslot(time + timeOffsetForNextSlot, allThreads, upcomingTasks)
                {
                    previousSlot = previous,
                };

                // Clear out our previous slot if there is one as only one previous slot needs to be known
                previousSlot = null;

                return nextSlot;
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

                AddRunningSystemData(systemToSchedule, thread);

                // Current system from thread is ran, step it to the next system
                thread.MarkExecutedTask(systemToSchedule);

                upcomingTasks.Remove(systemToSchedule);
            }

            private void AddRunningSystemData(SystemToSchedule systemToSchedule, Thread thread)
            {
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
            }

            private IEnumerable<SystemToSchedule> GetSystemsRunForThread(Thread thread)
            {
                if (!runSystems.TryGetValue(thread, out var list))
                    yield break;

                foreach (var systemToSchedule in list)
                {
                    yield return systemToSchedule;
                }
            }

            private void OnCombineWithPrevious()
            {
                if (previousSlot == null)
                    throw new InvalidOperationException("Must have previous slot");

                var realTime = previousSlot.time;

                if (realTime == time)
                    throw new Exception("Original time should be unique");

                Thread? runThread = null;

                // Remove extra barriers if safe to do so
                bool canRemoveBarrier = true;

                foreach (var pair in runSystems)
                {
                    if (pair.Value.Count < 1)
                        continue;

                    if (runThread == null)
                    {
                        runThread = pair.Key;
                    }
                    else if (runThread != pair.Key)
                    {
                        throw new Exception("Detected multiple ran threads");
                    }

                    // Barrier removal check
                    if (pair.Value.Last().RequiresBarrierAfter > 0)
                    {
                        // Find systems that would run without the barrier
                        var newSystemsThatWouldRun = new List<SystemToSchedule>();

                        // The barriers to look for before stopping is 3 as we expect to hit 1 immediately that is the
                        // one to be removed, then 1 from the slot that is going away, and finally the third barrier
                        // is the slot that is not to be combined, so it is left alone
                        int barriersToCheck = 3;

                        // Need to also add systems from the previous timeslot in case there is no barrier between

                        foreach (var systemToSchedule in previousSlot.GetSystemsRunForThread(pair.Key)
                                     .Concat(pair.Value).Reverse())
                        {
                            if (systemToSchedule.RequiresBarrierAfter > 0)
                            {
                                barriersToCheck -= systemToSchedule.RequiresBarrierAfter;

                                if (barriersToCheck <= 0)
                                    break;
                            }

                            newSystemsThatWouldRun.Add(systemToSchedule);
                        }

                        if (newSystemsThatWouldRun.Count < 1)
                            throw new Exception("Expected to find at least one new system that would run");

                        // Check if removing barrier from other threads would be safe to do so
                        // This needs to be 1 higher than when previously this variable was set to work correctly
                        barriersToCheck = 4;

                        foreach (var otherThread in allThreads)
                        {
                            if (otherThread == runThread)
                                continue;

                            foreach (var otherSystem in otherThread.GetAllExecutedTasks().AsEnumerable().Reverse())
                            {
                                if (otherSystem.RequiresBarrierAfter > 0)
                                {
                                    barriersToCheck -= otherSystem.RequiresBarrierAfter;

                                    // If seen enough barriers, things can no longer block removal of the barrier
                                    if (barriersToCheck <= 0)
                                        break;
                                }

                                for (int i = 0; i < newSystemsThatWouldRun.Count; ++i)
                                {
                                    if (newSystemsThatWouldRun[i].CanRunConcurrently(otherSystem))
                                        continue;

                                    // Removing a barrier would expose a conflict
                                    canRemoveBarrier = false;
                                    barriersToCheck = -1;

                                    // Or move them to the best place if possible
                                    if (i > 1)
                                    {
                                        // Can move the barrier a bit
                                        newSystemsThatWouldRun[i].RequiresBarrierAfter++;

                                        newSystemsThatWouldRun[0].RequiresBarrierAfter--;

                                        if (newSystemsThatWouldRun[0].RequiresBarrierAfter < 0)
                                            throw new Exception("Barrier move caused incorrect count");
                                    }

                                    break;
                                }

                                if (otherSystem.RequiresBarrierBefore > 0)
                                {
                                    barriersToCheck -= otherSystem.RequiresBarrierBefore;
                                }

                                if (barriersToCheck <= 0)
                                    break;
                            }
                        }
                    }
                    else
                    {
                        canRemoveBarrier = false;
                    }

                    foreach (var systemToSchedule in pair.Value)
                    {
                        // Add to previous (this should also reset the timeslot)
                        previousSlot.AddRunningSystemData(systemToSchedule, pair.Key);

                        if (systemToSchedule.Timeslot != realTime)
                            throw new Exception("Task timeslot update didn't apply");
                    }
                }

                foreach (var thread in allThreads)
                {
                    if (thread == runThread)
                        continue;

                    // Can't remove barrier if non-executed threads don't have enough after barriers
                    if (thread.GetAllExecutedTasks().Last().RequiresBarrierAfter < 3)
                    {
                        canRemoveBarrier = false;
                        break;
                    }
                }

                if (canRemoveBarrier)
                {
                    foreach (var thread in allThreads)
                    {
                        int barriersToRemove = 2;

                        foreach (var system in thread.GetAllExecutedTasks().AsEnumerable().Reverse())
                        {
                            while (system.RequiresBarrierAfter > 0 && barriersToRemove > 0)
                            {
                                --barriersToRemove;
                                --system.RequiresBarrierAfter;
                            }

                            if (barriersToRemove <= 0)
                                break;
                        }

                        if (barriersToRemove > 0)
                            throw new Exception("Couldn't find enough barriers to remove from thread");
                    }
                }
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
                if (threadTasks.Contains(system))
                    throw new ArgumentException("Already marked as executed by this thread");

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
