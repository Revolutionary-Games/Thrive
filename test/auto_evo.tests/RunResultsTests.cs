using System;
using System.Collections.Generic;
using System.Threading;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Checks that accepted mutations survive collection from concurrent patch steps.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class RunResultsTests
{
    /// <summary>
    ///   Submits distinct candidates with their parent's ID from concurrent producers and checks that every reference
    ///   and its population are retained exactly once after the producers finish, protecting against lost mutations.
    /// </summary>
    [TestCase]
    public void ConcurrentPossibleMutationsRetainEveryCandidate()
    {
        const int producerCount = 4;
        const int candidatesPerProducer = 2048;
        const int batchSize = 32;
        var results = new RunResults();
        var parent = new MicrobeSpecies(1, "Test", "parent");
        var patch = new Patch(new LocalizedString("Test"), 1,
            SimulationParameters.Instance.GetBiome("aavolcanic_vent"), BiomeType.Seafloor,
            new PatchRegion(1, "Test", PatchRegion.RegionType.Sea, default), 1);
        var candidates = new Species[producerCount * candidatesPerProducer];
        var expected = new Dictionary<Species, long>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < candidates.Length; ++i)
        {
            candidates[i] = new MicrobeSpecies(parent.ID, "Test", "candidate");
            expected.Add(candidates[i], i + 1);
        }

        // Isolate lost entries from concurrent resizing failures so this checks candidate retention specifically.
        results.GetPossibleSpeciesList().EnsureCapacity(candidates.Length);

        // Dedicated threads avoid starving the thread pool while the producers rendezvous between batches.
        var workers = new Thread[producerCount];
        var failures = new Exception?[producerCount];
        var barrier = new Barrier(producerCount);
        var cancellation = new CancellationTokenSource();
        int started = 0;
        bool drained = true;
        try
        {
            for (int i = 0; i < workers.Length; ++i)
            {
                int producer = i;
                workers[i] = new Thread(() =>
                {
                    try
                    {
                        for (int batch = 0; batch < candidatesPerProducer; batch += batchSize)
                        {
                            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10), cancellation.Token))
                                throw new TimeoutException("Candidate producers did not rendezvous");

                            for (int item = batch; item < batch + batchSize; ++item)
                            {
                                int index = producer * candidatesPerProducer + item;
                                results.AddPossibleMutation(candidates[index], new KeyValuePair<Patch, long>(patch,
                                    index + 1), RunResults.NewSpeciesType.SplitDueToMutation, parent);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        failures[producer] = e;
                        cancellation.Cancel();
                    }
                }) { IsBackground = true };
                workers[i].Start();
                ++started;
            }

            foreach (var worker in workers)
                drained &= worker.Join(TimeSpan.FromSeconds(30));
        }
        finally
        {
            cancellation.Cancel();
            bool cleanupCompleted = true;
            for (int i = 0; i < started; ++i)
                cleanupCompleted &= workers[i].Join(TimeSpan.FromSeconds(10));

            // Never dispose synchronisation objects while a failed worker could still use them.
            if (cleanupCompleted)
            {
                barrier.Dispose();
                cancellation.Dispose();
            }

            AssertThat(cleanupCompleted).IsTrue();
        }

        AssertThat(drained).IsTrue();
        foreach (var failure in failures)
            AssertThat(failure).IsNull();

        var collected = results.GetPossibleSpeciesList();
        AssertThat(collected.Count).IsEqual(candidates.Length);
        foreach (var candidate in collected)
        {
            AssertThat(expected.Remove(candidate.Species, out long population)).IsTrue();
            AssertThat(candidate.InitialPopulationInPatches.Key).IsSame(patch);
            AssertThat(candidate.InitialPopulationInPatches.Value).IsEqual(population);
            AssertThat(candidate.ParentSpecies).IsSame(parent);
            AssertThat(candidate.AddType).IsEqual(RunResults.NewSpeciesType.SplitDueToMutation);
        }

        AssertThat(expected.Count).IsEqual(0);
    }
}
