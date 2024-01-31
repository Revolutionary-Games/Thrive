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
            foreach (var thread in threadTasks)
            {
                threads.Add(new Thread(thread));
            }
        }

        public ThreadedRunSimulator(params IReadOnlyList<SystemToSchedule>[] threadTasks)
        {
            foreach (var thread in threadTasks)
            {
                threads.Add(new Thread(thread));
            }
        }

        public void Simulate()
        {
            // Setup simulation variables
            foreach (var thread in threads)
            {
                thread.Start();
            }

            int deadlockCounter = 0;

            // Step simulation until all threads are done
            while (true)
            {
                bool systemsActive = false;

                ++deadlockCounter;

                foreach (var thread in threads)
                {
                    if (thread.Done)
                        continue;

                    systemsActive = true;

                    if (thread.IsBlocked())
                    {
                        if (thread.WaitingFor == null)
                            throw new Exception("Should have been waiting for something");

                        // Check for unblocking
                        if (thread.CheckIsStillBlockedBy(thread.WaitingFor))
                            continue;
                    }

                    foreach (var thread2 in threads)
                    {
                        if (ReferenceEquals(thread, thread2))
                            continue;

                        // TODO: this probably doesn't work if we add more threads than 2
                        thread.CheckForInterferenceWith(thread2);
                    }

                    // Skip stepping if found interference
                    if (thread.IsBlocked())
                        continue;

                    // Step non-blocked / waiting threads
                    thread.Step();
                    deadlockCounter = 0;
                }

                if (!systemsActive)
                {
                    break;
                }

                if (deadlockCounter > 10000)
                {
                    throw new Exception("Simulated threads cannot progress, likely deadlocked");
                }
            }
        }

        private class Thread : IDisposable
        {
            public readonly IReadOnlyList<SystemToSchedule> ThreadTasks;
            public readonly List<Type> ActiveReads = new();
            public readonly List<Type> ActiveWrites = new();
            public readonly List<SystemToSchedule> RanSystems = new();

            private readonly SystemToSchedule.SystemRequirementsBasedComparer comparer = new();

            private IEnumerator<SystemToSchedule> enumerator;

            public Thread(IReadOnlyList<SystemToSchedule> threadTasks)
            {
                ThreadTasks = threadTasks;
                enumerator = threadTasks.GetEnumerator();
            }

            public enum RunConflictType
            {
                NoConflict,

                // NotInParallel,
                RunsBefore,
                RunsAfter,
            }

            public bool Done { get; private set; }

            public Thread? WaitingFor { get; private set; }

            public SystemToSchedule RunningSystem
            {
                get
                {
                    if (Done)
                        throw new InvalidOperationException("Already done");

                    return enumerator.Current!;
                }
            }

            public void Start()
            {
                ActiveReads.Clear();
                ActiveWrites.Clear();
                RanSystems.Clear();

                enumerator.Dispose();

                enumerator = ThreadTasks.GetEnumerator();
                Done = !enumerator.MoveNext();
                UpdateActiveSystem();
            }

            public bool Step()
            {
                if (Done)
                    return false;

                if (RunningSystem.RequiresBarrierAfter)
                {
                    ActiveWrites.Clear();
                    ActiveReads.Clear();
                    RanSystems.Clear();
                }

                Done = !enumerator.MoveNext();

                if (!Done)
                {
                    if (enumerator.Current == null)
                        throw new Exception("Enumerator item is null");
                }

                UpdateActiveSystem();

                return true;
            }

            public void MarkAsWaitingFor(Thread thread)
            {
                if (WaitingFor != null)
                    throw new InvalidOperationException("Already waiting for something");

                if (ReferenceEquals(thread, this))
                    throw new ArgumentException("Cannot wait for self");

                WaitingFor = thread;
            }

            public bool CheckIsStillBlockedBy(Thread thread)
            {
                if (WaitingFor != thread)
                    return true;

                if (HasRunConflictWith(thread) != RunConflictType.NoConflict)
                    return true;

                // No conflict anymore
                WaitingFor = null;
                return false;
            }

            public bool IsBlocked()
            {
                if (WaitingFor != null)
                    return true;

                return false;
            }

            public void CheckForInterferenceWith(Thread thread)
            {
                // If other thread is already blocked by us, don't need to consider it
                if (thread.WaitingFor == this)
                    return;

                var conflict = HasRunConflictWith(thread);

                if (conflict == RunConflictType.RunsBefore)
                {
                    // We run before the other one, block it
                    thread.MarkAsWaitingFor(this);
                    thread.AddBarrierBeforeCurrentSystem();
                    AddBarrierAfterCurrentSystem();
                }
                else if (conflict == RunConflictType.RunsAfter)
                {
                    // We are blocked by the other thread
                    MarkAsWaitingFor(thread);
                    thread.AddBarrierAfterCurrentSystem();
                    AddBarrierBeforeCurrentSystem();
                }
                /*else if (conflict == RunConflictType.NotInParallel)
                {
                    // For now earlier thread checking in the not parallel case gets priority
                    thread.MarkAsWaitingFor(this);
                    thread.AddBarrierBeforeCurrentSystem();
                    AddBarrierAfterCurrentSystem();
                }*/
            }

            public void AddBarrierBeforeCurrentSystem()
            {
                if (RunningSystem.RequiresBarrierBefore)
                    throw new InvalidOperationException("Barrier already added");

                RunningSystem.RequiresBarrierBefore = true;
                ActiveWrites.Clear();
                ActiveReads.Clear();
                RanSystems.Clear();
            }

            public void AddBarrierAfterCurrentSystem()
            {
                if (RunningSystem.RequiresBarrierAfter)
                    throw new InvalidOperationException("Barrier already added");

                RunningSystem.RequiresBarrierAfter = true;
            }

            public RunConflictType HasRunConflictWith(Thread thread)
            {
                if (thread.Done || Done)
                    return RunConflictType.NoConflict;

                // Mixed read / write conflict
                // TODO: should these also have the ordering property?
                if (ActiveReads.Any(t => thread.ActiveWrites.Contains(t)))
                    return RunConflictType.RunsAfter;

                if (ActiveWrites.Any(t => thread.ActiveReads.Contains(t)))
                    return RunConflictType.RunsBefore;

                int comparison;

                // Conflicting system run order
                foreach (var system1 in RanSystems)
                {
                    foreach (var system2 in thread.RanSystems)
                    {
                        comparison = comparer.CompareWeak(system1, system2);

                        if (comparison < 0)
                            return RunConflictType.RunsBefore;

                        if (comparison > 0)
                            return RunConflictType.RunsAfter;
                    }
                }

                // Also check blocked thread systems
                comparison = comparer.CompareWeak(RunningSystem, thread.RunningSystem);

                if (comparison < 0)
                    return RunConflictType.RunsBefore;

                if (comparison > 0)
                    return RunConflictType.RunsAfter;

                return RunConflictType.NoConflict;
            }

            public void Dispose()
            {
                enumerator.Dispose();
            }

            private void UpdateActiveSystem()
            {
                if (Done)
                {
                    ActiveReads.Clear();
                    ActiveWrites.Clear();
                    RanSystems.Clear();
                    return;
                }

                ActiveReads.AddRange(RunningSystem.ReadsComponents);
                ActiveWrites.AddRange(RunningSystem.WritesComponents);
                RanSystems.Add(RunningSystem);
            }
        }
    }
}
