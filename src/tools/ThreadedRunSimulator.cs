﻿namespace Tools
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
        private const double ChanceForNoWorkPerAheadTime = 0.5;
        private const double ChanceToShuffleThreadOrderOnTimeStep = 0.3;

        /// <summary>
        ///   Relative time cost of a barrier to running an average system
        /// </summary>
        private const float TimeCostPerBarrier = 1.5f;

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

            GD.Print($"Simulated {attempts} thread orderings to find best one");

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

            private bool HasThreadsWithExclusiveTasks => allThreads.Any(t => t.NextExclusiveTask != null);

            public (float TotalTime, float ThreadDifference) AttemptOrdering(int seed)
            {
                Reset();
                random = new Random(seed);
                upcomingGeneralTasks.Clear();
                upcomingGeneralTasks.AddRange(originalGeneralTasks);

                // TODO: chance to shuffle or shuffle and weak order the general tasks

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

                    // If cannot schedule anything (and there's something to do), need to move to next group of barriers
                    if (HasUpcomingTasks)
                    {
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

                var result = new List<List<SystemToSchedule>>();

                foreach (var thread in allThreads)
                {
                    result.Add(thread.GetAndApplyResult());
                }

                return result;
            }

            private void Reset()
            {
                foreach (var thread in allThreads)
                {
                    thread.Reset();
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

                if (ahead > 0)
                {
                    if (random.NextDouble() < ChanceForNoWorkPerAheadTime * ahead)
                        return false;
                }

                var exclusiveTask = thread.NextExclusiveTask;

                // Prioritize running exclusive tasks a bit
                if (exclusiveTask != null)
                {
                    if (random.NextDouble() < ExclusiveTaskChance)
                    {
                        if (CanRunSystemInParallel(exclusiveTask, thread))
                        {
                            MarkConcurrentlyRunningSystem(exclusiveTask, thread);
                            return true;
                        }
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
                if (!CanRunSystemInParallel(systemToSchedule, thread))
                    throw new InvalidOperationException("Cannot run the system in parallel");

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
            private readonly IReadOnlyList<SystemToSchedule> exclusiveTasks;

            private readonly List<int> threadBarrierPoints = new();

            private int nextExclusiveTaskIndex;

            public Thread(int threadId, IReadOnlyList<SystemToSchedule> exclusiveTasks)
            {
                ThreadId = threadId;
                this.exclusiveTasks = exclusiveTasks;
            }

            public int ThreadId { get; }

            public float Time { get; private set; }

            /// <summary>
            ///   Time since last barrier, used to not schedule too much work for a single thread
            /// </summary>
            public float TimeSinceBarrier { get; private set; }

            public SystemToSchedule? NextExclusiveTask => nextExclusiveTaskIndex < exclusiveTasks.Count ?
                exclusiveTasks[nextExclusiveTaskIndex] :
                null;

            public IEnumerable<SystemToSchedule> GetStillUpcomingSystems()
            {
                return exclusiveTasks.Skip(nextExclusiveTaskIndex);
            }

            public void MarkExecutedTask(SystemToSchedule system)
            {
                if (threadTasks.Contains(system))
                    throw new ArgumentException("Already marked as executed by this thread");

                threadTasks.Add(system);

                if (exclusiveTasks.Contains(system))
                {
                    if (NextExclusiveTask == system)
                    {
                        ++nextExclusiveTaskIndex;
                    }
                    else
                    {
                        throw new Exception("Exclusive task ordering was not followed");
                    }
                }

                // Move forward in time, this thread is busy for the time the system takes estimated to run
                Time += system.RuntimeCost;
                TimeSinceBarrier += system.RuntimeCost;
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

            public void Reset()
            {
                threadTasks.Clear();
                threadBarrierPoints.Clear();

                Time = 0;
                TimeSinceBarrier = 0;
                nextExclusiveTaskIndex = 0;

                // TODO: chance to shuffle or shuffle and weak order the specific tasks (would need a separate list)?
            }

            public override string ToString()
            {
                return $"Thread {ThreadId} at time {Time}";
            }
        }
    }
}
