namespace Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Godot;

    /// <summary>
    ///   Simulates running <see cref="SystemToSchedule"/> accross multiple allThreads and generates barriers for
    ///   correctness
    /// </summary>
    public class ThreadedRunSimulator
    {
        private const double ExclusiveTaskChance = 0.90;
        private const double ChanceToShuffleThreadOrderOnTimeStep = 0.3;
        private const double ExclusiveContinueCheckChance = 0.95;
        private const double SkipThreadWorkChance = 0.01;
        private const double MoveSingleItemToOtherThreadChance = 0.30;

        // TODO: currently this doesn't really impact anything (or very unlikely for this to impact anything)
        private const double ChanceForNoWorkPerAheadTime = 0.5;

        private const double ChanceToShuffleExclusiveTasks = 0.1;
        private const double ChanceToShuffleNormalTasks = 0.2;

        /// <summary>
        ///   Relative time cost of a barrier to running an average system (which is 1). For example 1.5 means that a
        ///   barrier is as expensive as running 1.5 other systems.
        /// </summary>
        private const float TimeCostPerBarrier = 1.5f;

        private static readonly bool ExtraVerifySystemRun = false;

        /// <summary>
        ///   After finding a new best result, how long to look at more attempts
        /// </summary>
        private readonly TimeSpan timeToLookForMoreResults = TimeSpan.FromSeconds(10);

        private readonly object resultLock = new();

        private readonly IReadOnlyList<SystemToSchedule> freelyAssignableTasks;
        private readonly int threadCount;
        private readonly IReadOnlyList<SystemToSchedule> mainThreadTasks;

        private DateTime lastNewBestFound;
        private float bestThreadTime;
        private float bestThreadDifference;
        private SimulationAttempt? bestSimulation;
        private int attempts;

        public ThreadedRunSimulator(IReadOnlyList<SystemToSchedule> mainThreadTasks,
            IReadOnlyList<SystemToSchedule> freelyAssignableTasks, int threadCount)
        {
            if (threadCount < 1)
                throw new ArgumentException("Must have at least one thread");

            this.freelyAssignableTasks = freelyAssignableTasks;
            this.threadCount = threadCount;
            this.mainThreadTasks = mainThreadTasks;
        }

        /// <summary>
        ///   Simulate threads and find a good ordering
        /// </summary>
        /// <returns>The tasks split into threads, first item is always the main thread tasks</returns>
        public List<List<SystemToSchedule>> Simulate(int initialSeed, int parallelTasks)
        {
            if (parallelTasks < 1)
                throw new ArgumentException("Parallel task count needs to be at least 1");

            var random = new Random(initialSeed);

            // Reset state in case this is reused
            bestThreadTime = float.MaxValue;
            bestThreadDifference = float.MaxValue;
            bestSimulation = null;

            attempts = 0;

            // Create parallel tasks
            var tasks = new List<Task>();
            for (int i = 0; i < parallelTasks; ++i)
            {
                int seed = random.Next();
                tasks.Add(new Task(() => RunSimulationAttempts(seed)));
            }

            lock (resultLock)
            {
                lastNewBestFound = DateTime.UtcNow;
            }

            // Wait for tasks to end, this is not time critical threading here so this thread can be used to run
            // a bunch more tasks
            TaskExecutor.Instance.RunTasks(tasks, true);

            if (bestSimulation == null)
                throw new Exception("Couldn't find any valid thread orderings");

            GD.Print($"Simulated {attempts} thread orderings to find the best one");

            return bestSimulation.ToTaskListResult();
        }

        private void RunSimulationAttempts(int seed)
        {
            var random = new Random(seed);
            SimulationAttempt? currentSimulationAttempt = null;

            while (AttemptMoreSimulations())
            {
                currentSimulationAttempt ??= new SimulationAttempt(freelyAssignableTasks, mainThreadTasks,
                    threadCount);

                var (currentTime, currentDifference) = currentSimulationAttempt.AttemptOrdering(random.Next());
                Interlocked.Increment(ref attempts);

                // This is a bit cumbersome condition, but needed due to equal value being good enough if the thread
                // deviance is smaller
                if (!(currentTime <= bestThreadTime))
                    continue;

                lock (resultLock)
                {
                    if (currentTime < bestThreadTime || currentDifference < bestThreadDifference)
                    {
                        bestThreadTime = currentTime;
                        bestThreadDifference = currentDifference;
                        lastNewBestFound = DateTime.UtcNow;

                        GD.Print($"Found new best thread simulation: {bestThreadTime} " +
                            $"thread variance: {bestThreadDifference}");

                        // Store the current attempt as the best one, and set it to null as we need to create a new
                        // object to keep testing attempts
                        bestSimulation = currentSimulationAttempt;
                        currentSimulationAttempt = null;
                    }
                }
            }
        }

        private bool AttemptMoreSimulations()
        {
            lock (resultLock)
            {
                return DateTime.UtcNow - lastNewBestFound < timeToLookForMoreResults;
            }
        }

        /// <summary>
        ///   Attempted ordering of thread simulation. A bunch of these are attempted with different seeds to find a
        ///   good thread task assignment and barrier locations.
        /// </summary>
        private class SimulationAttempt
        {
            private readonly IReadOnlyList<SystemToSchedule> originalGeneralTasks;

            private readonly List<Thread> allThreads = new();
            private readonly List<SystemToSchedule> upcomingGeneralTasks = new();

            // All reads / runs need to be stored per-thread so when one thread is blocked another can still
            private readonly Dictionary<Thread, HashSet<Type>> componentReads = new();
            private readonly Dictionary<Thread, HashSet<Type>> componentWrites = new();
            private readonly Dictionary<Thread, List<SystemToSchedule>> runSystems = new();

            private readonly SystemToSchedule.SystemRequirementsBasedComparer comparer = new();

            private Random random = new();

            public SimulationAttempt(IReadOnlyList<SystemToSchedule> generalTasks,
                IReadOnlyList<SystemToSchedule> mainThreadTasks, int threadCount)
            {
                originalGeneralTasks = generalTasks;

                for (int i = 0; i < threadCount; ++i)
                {
                    // Main tasks are reserved for first thread
                    var thread = new Thread(i + 1, i == 0 ? mainThreadTasks : new List<SystemToSchedule>());
                    allThreads.Add(thread);
                }
            }

            private bool HasUpcomingTasks => upcomingGeneralTasks.Count > 0 || HasThreadsWithExclusiveTasks;

            private bool HasThreadsWithExclusiveTasks => allThreads.Any(t => t.HasUpcomingExclusiveTask);

            public (float TotalTime, float ThreadDifference) AttemptOrdering(int seed)
            {
                random = new Random(seed);
                Reset(random.NextDouble() < ChanceToShuffleExclusiveTasks);

                upcomingGeneralTasks.Clear();
                upcomingGeneralTasks.AddRange(originalGeneralTasks);

                if (random.NextDouble() < ChanceToShuffleNormalTasks)
                {
                    upcomingGeneralTasks.Shuffle(random);

                    // TODO: should this re-apply at least some sorting?
                }

                if (!HasUpcomingTasks)
                    throw new Exception("Failed to add tasks to simulate");

                // How long since the last barrier (as barriers synchronize the times of threads, this is used instead
                // of total simulated time)
                double timeSinceBarrier = 0;
                float extraTimeCost = 0;
                int stuckCount = 0;

                while (HasUpcomingTasks)
                {
                    bool scheduledSomething = false;

                    double neededTimeSkip = double.MaxValue;
                    bool anyThreadIsAtCurrentTime = false;
                    bool skippedThread = false;

                    foreach (var thread in allThreads)
                    {
                        var timeInFuture = thread.TimeSinceBarrier - timeSinceBarrier;

                        // If thread is still busy at this point in time, can't do anything
                        if (timeInFuture > 0)
                        {
                            if (neededTimeSkip > timeInFuture)
                                neededTimeSkip = timeInFuture;

                            continue;
                        }

                        anyThreadIsAtCurrentTime = true;

                        // Small chance to skip giving task to a thread to explore more options
                        if (random.NextDouble() < SkipThreadWorkChance)
                        {
                            skippedThread = true;
                            continue;
                        }

                        // Thread is free, try to schedule work
                        if (ScheduleWorkForThread(thread))
                        {
                            scheduledSomething = true;
                        }
                    }

                    // Keep happily scheduling stuff until we can't find anything to schedule
                    if (scheduledSomething)
                    {
                        stuckCount = 0;
                        continue;
                    }

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (!anyThreadIsAtCurrentTime && neededTimeSkip != double.MaxValue)
                    {
                        // All threads are still busy, jump forward in time to when the next thread is ready to do
                        // something

                        timeSinceBarrier += neededTimeSkip + MathUtils.EPSILON;

                        if (random.NextDouble() < ChanceToShuffleThreadOrderOnTimeStep)
                            RandomizeThreadOrder();

                        continue;
                    }

                    // When skipping threads needs to re-process the current thread situation until no threads are
                    // skipped
                    if (skippedThread)
                        continue;

                    // If cannot schedule anything (and there's something to do), need to move to next group of barriers
                    if (HasUpcomingTasks)
                    {
                        // If there are other threads with barely any work to do, there's a chance to steal their
                        // work for another thread
                        bool attemptTaskSteal = true;

                        for (int i = 1; i < allThreads.Count; ++i)
                        {
                            if (runSystems.TryGetValue(allThreads[i], out var systems))
                            {
                                if (systems.Count > 1)
                                    attemptTaskSteal = false;
                            }
                        }

                        if (attemptTaskSteal && random.NextDouble() < MoveSingleItemToOtherThreadChance)
                        {
                            // Can steal work, don't need to add a barrier (if successful)
                            if (StealWorkFromOtherThreads(allThreads[0]))
                                continue;
                        }

                        AddBarrierPoint();
                        timeSinceBarrier = 0;
                        stuckCount = 0;

                        // Barriers cost extra time
                        extraTimeCost += TimeCostPerBarrier;
                    }
                    else
                    {
                        ++stuckCount;

                        if (stuckCount > 100)
                            throw new Exception("Thread simulation is stuck");
                    }
                }

                return (allThreads.Max(t => t.Time) + extraTimeCost, CalculateThreadTimeDeviance());
            }

            public List<List<SystemToSchedule>> ToTaskListResult()
            {
                // Need to sort threads back to have main thread first
                allThreads.Sort(new ThreadIdComparer());

                if (allThreads[0].ThreadId != 1)
                    throw new Exception("Thread order sort back to normal failed");

                // Remove any extra barriers that can be removed
                AnalyzeAndRemoveExtraBarriers();

                var result = new List<List<SystemToSchedule>>();

                foreach (var thread in allThreads)
                {
                    result.Add(thread.GetAndApplyResult());
                }

                return result;
            }

            private void Reset(bool shuffleExclusiveTasks)
            {
                foreach (var thread in allThreads)
                {
                    thread.Reset(shuffleExclusiveTasks, random);
                }

                ClearActiveSystemsAndComponentUses();
            }

            private float CalculateThreadTimeDeviance()
            {
                return (float)allThreads.Select(t => (double)t.Time).CalculateAverageAndStandardDeviation()
                    .StandardDeviation;
            }

            private void AddBarrierPoint()
            {
                foreach (var thread in allThreads)
                {
                    thread.AddBarrier();
                }

                ClearActiveSystemsAndComponentUses();
            }

            private void RandomizeThreadOrder()
            {
                allThreads.Shuffle(random);
            }

            private void ClearActiveSystemsAndComponentUses()
            {
                foreach (var pair in componentReads)
                {
                    pair.Value.Clear();
                }

                foreach (var pair in componentWrites)
                {
                    pair.Value.Clear();
                }

                foreach (var pair in runSystems)
                {
                    pair.Value.Clear();
                }
            }

            private bool ScheduleWorkForThread(Thread thread)
            {
                // If thread is ahead other threads, it gets more unlikely to be given more work
                var ahead = allThreads.Where(t => t != thread)
                    .Average(t => thread.TimeSinceBarrier - t.TimeSinceBarrier);

                // TODO: maybe remove this code entirely as thanks to the thread being executing in the future checks
                // no threads that are ahead other ones really get even into this method in the first place
                if (ahead > 0)
                {
                    if (random.NextDouble() < ChanceForNoWorkPerAheadTime * ahead)
                        return false;
                }

                // Prioritize running exclusive tasks a bit
                bool triedExclusive = false;
                if (thread.HasUpcomingExclusiveTask && random.NextDouble() < ExclusiveTaskChance)
                {
                    triedExclusive = true;

                    foreach (var exclusiveTask in thread.GetUpcomingExclusiveTasks())
                    {
                        if (CanRunSystemInParallel(exclusiveTask, thread))
                        {
                            MarkConcurrentlyRunningSystem(exclusiveTask, thread);
                            return true;
                        }

                        // Chance to stop checking early
                        if (random.NextDouble() > ExclusiveContinueCheckChance)
                            break;
                    }
                }

                // Then try the general pool
                foreach (var task in upcomingGeneralTasks)
                {
                    if (CanRunSystemInParallel(task, thread))
                    {
                        MarkConcurrentlyRunningSystem(task, thread);
                        return true;
                    }

                    // TODO: should this have a chance here to stop checking things for more randomness?
                }

                // If skipped earlier, try an exclusive task again
                if (thread.HasUpcomingExclusiveTask && !triedExclusive)
                {
                    foreach (var exclusiveTask in thread.GetUpcomingExclusiveTasks())
                    {
                        if (CanRunSystemInParallel(exclusiveTask, thread))
                        {
                            MarkConcurrentlyRunningSystem(exclusiveTask, thread);
                            return true;
                        }
                    }
                }

                // Thread cannot start work
                return false;
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
                    // Should not skip checking self as later exclusive systems shouldn't be allowed to run on the
                    // same thread either

                    foreach (var futureSystem in otherThread.GetUpcomingExclusiveTasks())
                    {
                        if (comparer.CompareWeak(systemToSchedule, futureSystem) > 0)
                        {
                            // Need to wait for this thread to manage to run this task
                            return false;
                        }
                    }
                }

                foreach (var upcomingTask in upcomingGeneralTasks)
                {
                    if (comparer.CompareWeak(systemToSchedule, upcomingTask) > 0)
                    {
                        // Need to wait until some thread takes this upcoming task before the checked task can be run
                        return false;
                    }
                }

                // No conflicts with other things happening in this barrier section
                return true;
            }

            private void MarkConcurrentlyRunningSystem(SystemToSchedule systemToSchedule, Thread thread)
            {
                // This check is not usually required as this is only called after the caller checks that this is
                // allowed to run
                if (ExtraVerifySystemRun)
                {
                    if (!CanRunSystemInParallel(systemToSchedule, thread))
                        throw new InvalidOperationException("Cannot run the system in parallel");
                }

                AddRunningSystemData(systemToSchedule, thread);

                // Current system from thread is ran, step it to the next system
                thread.MarkExecutedTask(systemToSchedule);

                upcomingGeneralTasks.Remove(systemToSchedule);
            }

            private void AddRunningSystemData(SystemToSchedule systemToSchedule, Thread thread)
            {
                // Running systems within the current barrier slot
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
            }

            /// <summary>
            ///   Removes a system from a thread and recalculates the component data for that system. For use with
            ///   task stealing.
            /// </summary>
            private void RemoveAndRebuildRunningSystemData(SystemToSchedule systemToRemove, Thread thread)
            {
                var newSystems = thread.GetTasksSinceLastBarrier();

                if (newSystems.Contains(systemToRemove))
                    throw new InvalidOperationException("System should already be removed from the thread");

                if (runSystems.TryGetValue(thread, out var threadSystems))
                {
                    if (!threadSystems.Remove(systemToRemove))
                        throw new Exception("Failed to remove system");
                }
                else
                {
                    throw new Exception("Failed to remove system");
                }

                // Component writes and reads
                if (!componentReads.TryGetValue(thread, out var threadReads))
                {
                    throw new Exception("No reads to modify");
                }

                if (!componentWrites.TryGetValue(thread, out var threadWrites))
                {
                    throw new Exception("No writes to modify");
                }

                threadReads.Clear();
                threadWrites.Clear();

                foreach (var system in newSystems)
                {
                    foreach (var component in system.ReadsComponents)
                    {
                        threadReads.Add(component);
                    }

                    foreach (var component in system.WritesComponents)
                    {
                        threadWrites.Add(component);
                    }
                }
            }

            private bool StealWorkFromOtherThreads(Thread targetThread)
            {
                bool stoleWork = false;

                foreach (var thread in allThreads)
                {
                    if (thread == targetThread)
                        continue;

                    if (!runSystems.TryGetValue(thread, out var potentialSystems) || potentialSystems.Count < 1)
                        continue;

                    if (potentialSystems.Count > 1)
                        throw new Exception("Cannot take more than one task");

                    var takenSystem = potentialSystems[0];

                    // Cannot steal an exclusive task
                    if (thread.IsExclusiveTask(takenSystem))
                        continue;

                    thread.ReportStolenTask(takenSystem);
                    RemoveAndRebuildRunningSystemData(takenSystem, thread);

                    if (!CanRunSystemInParallel(takenSystem, targetThread))
                        throw new Exception("Target thread cannot take stolen task");

                    MarkConcurrentlyRunningSystem(takenSystem, targetThread);
                    stoleWork = true;
                }

                return stoleWork;
            }

            private void AnalyzeAndRemoveExtraBarriers()
            {
                var runningThreadCountsPerSlot = new List<int>();

                // Count how many threads are active between which barriers to determine where there are places with
                // unnecessary barriers that don't guard against anything
                foreach (var thread in allThreads)
                {
                    int index = 0;

                    foreach (var count in thread.CalculateTasksBetweenBarriers())
                    {
                        while (runningThreadCountsPerSlot.Count <= index)
                            runningThreadCountsPerSlot.Add(0);

                        // With more than 0 tasks executed, the thread is considered active
                        if (count > 0)
                        {
                            runningThreadCountsPerSlot[index] += 1;
                        }

                        ++index;
                    }
                }

                var pointIndexesToRemove = new List<int>();

                for (int i = 0; i < runningThreadCountsPerSlot.Count - 1; ++i)
                {
                    if (runningThreadCountsPerSlot[i] < 2 && runningThreadCountsPerSlot[i + 1] < 2)
                    {
                        // Found two consecutive barriers where the earlier one should be fine to remove
                        // The earlier slot either has multiple threads and protects this slot that way, or it is also
                        // one thread slot and is safe to merge into this one.
                        pointIndexesToRemove.Add(i);
                    }
                }

                // The remove list is reversed so that indexes stay valid as things are being deleted
                pointIndexesToRemove.Reverse();

                foreach (var barrierToRemove in pointIndexesToRemove)
                {
                    GD.Print("Removing extra barrier at barrier index " + barrierToRemove);

                    foreach (var thread in allThreads)
                    {
                        thread.RemoveBarrierWithIndex(barrierToRemove);
                    }
                }
            }

            private class ThreadIdComparer : IComparer<Thread>
            {
                public int Compare(Thread x, Thread y)
                {
                    if (ReferenceEquals(x, y))
                        return 0;
                    if (ReferenceEquals(null, y))
                        return 1;
                    if (ReferenceEquals(null, x))
                        return -1;

                    return x.ThreadId.CompareTo(y.ThreadId);
                }
            }
        }

        private class Thread
        {
            private readonly List<SystemToSchedule> threadTasks = new();
            private readonly List<SystemToSchedule> upcomingExclusiveTasks = new();

            private readonly IReadOnlyList<SystemToSchedule> originalExclusiveTasks;

            private readonly List<int> threadBarrierPoints = new();

            public Thread(int threadId, IReadOnlyList<SystemToSchedule> exclusiveTasks)
            {
                ThreadId = threadId;
                originalExclusiveTasks = exclusiveTasks;

                Reset(false, null);
            }

            public int ThreadId { get; }

            public float Time { get; private set; }

            /// <summary>
            ///   Time since last barrier, used to not schedule too much work for a single thread
            /// </summary>
            public float TimeSinceBarrier { get; private set; }

            public bool HasUpcomingExclusiveTask => upcomingExclusiveTasks.Count > 0;

            public IEnumerable<SystemToSchedule> GetUpcomingExclusiveTasks()
            {
                return upcomingExclusiveTasks;
            }

            public void MarkExecutedTask(SystemToSchedule system)
            {
                if (threadTasks.Contains(system))
                    throw new ArgumentException("Already marked as executed by this thread");

                threadTasks.Add(system);

                bool removedExclusive = upcomingExclusiveTasks.Remove(system);

                if (!removedExclusive && originalExclusiveTasks.Contains(system))
                    throw new Exception("Exclusive task remove was not done");

                // Move forward in time, this thread is busy for the time the system takes estimated to run
                Time += system.RuntimeCost;
                TimeSinceBarrier += system.RuntimeCost;
            }

            public bool IsExclusiveTask(SystemToSchedule system)
            {
                return originalExclusiveTasks.Contains(system);
            }

            public void ReportStolenTask(SystemToSchedule system)
            {
                if (threadTasks.IndexOf(system) != threadTasks.Count - 1)
                    throw new ArgumentException("Can only steal latest task");

                if (originalExclusiveTasks.Contains(system))
                    throw new ArgumentException("Cannot steal an exclusive task");

                if (!threadTasks.Remove(system))
                    throw new ArgumentException("Failed to remove task from executed");

                // Move back in time as if the system never ran
                Time -= system.RuntimeCost;

                // Recount the time since last barrier
                if (threadBarrierPoints.Count < 1)
                {
                    // When no barriers, all costs are still active and can be adjusted like this
                    TimeSinceBarrier -= system.RuntimeCost;

                    if (TimeSinceBarrier < 0)
                        throw new Exception("Time since barrier recalculation problem");

                    return;
                }

                // If barrier was at the last point (which is now gone), remove it
                threadBarrierPoints.RemoveAll(p => p == threadTasks.Count);

                int lastBarrier = threadBarrierPoints.Max();

                TimeSinceBarrier = 0;

                for (int i = lastBarrier + 1; i < threadTasks.Count; ++i)
                {
                    TimeSinceBarrier += threadTasks[i].RuntimeCost;
                }
            }

            public void AddBarrier()
            {
                TimeSinceBarrier = 0;

                threadBarrierPoints.Add(threadTasks.Count - 1);
            }

            public List<SystemToSchedule> GetAndApplyResult()
            {
                if (threadTasks.Count < 1)
                    throw new InvalidOperationException("Thread doesn't have any tasks");

                int timeSlot = 1;

                for (int i = 0; i < threadTasks.Count; ++i)
                {
                    var systemToSchedule = threadTasks[i];
                    systemToSchedule.ThreadId = ThreadId;

                    // Assign timeslots based on barriers
                    systemToSchedule.Timeslot = timeSlot;

                    timeSlot += threadBarrierPoints.Count(b => b == i);
                }

                foreach (var barrierPoint in threadBarrierPoints)
                {
                    // Barriers after the systems have run are assumed to be for the last system
                    if (barrierPoint > threadTasks.Count)
                    {
                        ++threadTasks[threadTasks.Count - 1].RequiresBarrierAfter;
                    }
                    else
                    {
                        ++threadTasks[barrierPoint].RequiresBarrierAfter;
                    }
                }

                return threadTasks;
            }

            public IEnumerable<int> CalculateTasksBetweenBarriers()
            {
                if (!threadBarrierPoints.AsEnumerable().OrderBy(i => i).SequenceEqual(threadBarrierPoints))
                    throw new Exception("Barrier points aren't in sorted order");

                // This has to start at -1 for the count of tasks in the first barrier group to be correct
                int previousBarrier = -1;

                foreach (var barrierPoint in threadBarrierPoints)
                {
                    int tasksBetween = barrierPoint - previousBarrier;

                    yield return tasksBetween;

                    previousBarrier = barrierPoint;
                }
            }

            public void RemoveBarrierWithIndex(int barrierIndexToRemove)
            {
                threadBarrierPoints.RemoveAt(barrierIndexToRemove);
            }

            public IReadOnlyList<SystemToSchedule> GetTasksSinceLastBarrier()
            {
                // Can just return everything when no barriers
                if (threadBarrierPoints.Count < 1)
                    return threadTasks;

                int lastBarrier = threadBarrierPoints.Max();

                var result = new List<SystemToSchedule>();

                for (int i = lastBarrier + 1; i < threadTasks.Count; ++i)
                {
                    result.Add(threadTasks[i]);
                }

                return result;
            }

            public void Reset(bool shuffle, Random? random)
            {
                threadTasks.Clear();
                threadBarrierPoints.Clear();

                Time = 0;
                TimeSinceBarrier = 0;

                upcomingExclusiveTasks.Clear();
                upcomingExclusiveTasks.AddRange(originalExclusiveTasks);

                if (shuffle && upcomingExclusiveTasks.Count > 0)
                {
                    upcomingExclusiveTasks.Shuffle(random ?? new Random());
                }
            }

            public override string ToString()
            {
                return $"Thread {ThreadId} at time {Time}";
            }
        }
    }
}
