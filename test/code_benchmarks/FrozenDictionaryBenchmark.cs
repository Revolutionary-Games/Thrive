namespace CodeBenchmark;

using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class FrozenDictionaryBenchmark
{
    [Params(0, 0.01f, 0.1f, 0.9f)]
    public float NonSequentialReads;

    [Params(5000000)]
    public int Reads;

    // Some approaches totally collapse with more threads added (so need to test this)
    [Params(0, 1, 2)]
    public int ExtraReaderThreads;

    private const int CLOUD_PLANE_SQUARES_PER_SIDE = 3;
    private const int CLOUD_SIZE = 300;

    private const int RandomSeed = 422;

    private static readonly int[] PlaneOffsets = [0, 100, 200];
    private static readonly int[] PlayerPositions = [0, 1, 2];

    // Not const so that this can theoretically change, which would impact optimizations
    private static int cloudResolution = 2;

    private Vector2 cachedWorldPosition = new(100, 100);

    private Dictionary<int, Vector2> sourceData = null!;
    private Vector2[] sourceArray = null!;

    [GlobalSetup]
    public void Setup()
    {
        sourceData = PrecalculateWorldShiftVectors();
        sourceArray = CalculateArrayCache();
    }

    [Benchmark]
    public void RecalculateKeyEachTime()
    {
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int x, int y, int playerX, int playerY)
        {
            var recalculated = CalculateShift(PlaneOffsets[x], PlaneOffsets[y], PlayerPositions[playerX],
                PlayerPositions[playerY]);

            /*var key = GetIndexKey(x, y, playerX, playerY);
            if (!sourceData.TryGetValue(key, out var cached))
                throw new Exception("Logic error");

            if (recalculated != cached)
                throw new Exception("Logic error!");*/

            _ = cachedWorldPosition + recalculated;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    // Random is used outside the configuration so that random reads and sequential reads have a similar
                    // constant overhead.
                    // Putting this inside the if-statement significantly changes the results, with the direct
                    // computation being favoured quite a lot.
                    var (x, y, playerX, playerY) = GetRandomIndexes(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(x, y, playerX, playerY);
                    }
                    else
                    {
                        TryRead(xRead, yRead, playerXRead, playerYRead);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var (x, y, playerX, playerY) = GetRandomIndexes(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(x, y, playerX, playerY);
                }
                else
                {
                    TryRead(xRead, yRead, playerXRead, playerYRead);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void DictionaryTryGetValue()
    {
        var dictionary = sourceData.ToDictionary();
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            if (!dictionary.TryGetValue(key, out var cached))
                throw new Exception("Logic error");

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var key = GetRandomKey(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                        TryRead(indexedKey);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var key = GetRandomKey(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                    TryRead(indexedKey);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void DictionaryMarshalGetRef()
    {
        var dictionary = sourceData.ToDictionary();
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            ref readonly var cached = ref CollectionsMarshal.GetValueRefOrNullRef(dictionary, key);
            if (Unsafe.IsNullRef(in cached))
                throw new Exception("Logic error");

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var key = GetRandomKey(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                        TryRead(indexedKey);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var key = GetRandomKey(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                    TryRead(indexedKey);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void FrozenDictionaryTryGet()
    {
        var dictionary = sourceData.ToFrozenDictionary();
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            if (!dictionary.TryGetValue(key, out var cached))
                throw new Exception("Logic error");

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var key = GetRandomKey(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                        TryRead(indexedKey);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var key = GetRandomKey(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                    TryRead(indexedKey);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void FrozenDictionaryGetValueRef()
    {
        var dictionary = sourceData.ToFrozenDictionary();
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            ref readonly var cached = ref dictionary.GetValueRefOrNullRef(key);
            if (Unsafe.IsNullRef(in cached))
                throw new Exception("Logic error");

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var key = GetRandomKey(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                        TryRead(indexedKey);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var key = GetRandomKey(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                    TryRead(indexedKey);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void FrozenDictionaryDirectRandomTry()
    {
        var dictionary = sourceData.ToFrozenDictionary();
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            if (!dictionary.TryGetValue(key, out var cached))
                throw new Exception("Logic error");

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var key = GetRandomKeyDirect(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                        TryRead(indexedKey);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var key = GetRandomKeyDirect(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                    TryRead(indexedKey);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void FrozenDictionaryDirectRandomRef()
    {
        var dictionary = sourceData.ToFrozenDictionary();
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            ref readonly var cached = ref dictionary.GetValueRefOrNullRef(key);
            if (Unsafe.IsNullRef(in cached))
                throw new Exception("Logic error");

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var key = GetRandomKeyDirect(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                        TryRead(indexedKey);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var key = GetRandomKeyDirect(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    var indexedKey = GetIndexKey(xRead, yRead, playerXRead, playerYRead);
                    TryRead(indexedKey);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    // For some reason this is a much slower version than ArrayFlatIndex
    [Benchmark]
    public void FlatArray()
    {
        var array = sourceArray;
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            var cached = array[key];

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    var (x, y, playerX, playerY) = GetRandomIndexes(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(GetFlatArrayIndex(x, y, playerX, playerY));
                    }
                    else
                    {
                        var index = GetFlatArrayIndex(xRead, yRead, playerXRead, playerYRead);
                        TryRead(index);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                var (x, y, playerX, playerY) = GetRandomIndexes(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(GetFlatArrayIndex(x, y, playerX, playerY));
                }
                else
                {
                    var index = GetFlatArrayIndex(xRead, yRead, playerXRead, playerYRead);
                    TryRead(index);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    [Benchmark]
    public void FlatArrayDirectRandom()
    {
        var array = sourceArray;
        var tasks = new Task[ExtraReaderThreads];

        void TryRead(int key)
        {
            var cached = array[key];

            _ = cachedWorldPosition + cached;
        }

        for (int task = 0; task < ExtraReaderThreads; ++task)
        {
            tasks[task] = Task.Run(() =>
            {
                var random = new Random(RandomSeed);
                int xRead = 0;
                int yRead = 0;
                int playerXRead = 0;
                int playerYRead = 0;

                for (int i = 0; i < Reads; ++i)
                {
                    int key = GetRandomKeyArray(random);
                    if (random.NextDouble() < NonSequentialReads)
                    {
                        TryRead(key);
                    }
                    else
                    {
                        int index = GetFlatArrayIndex(xRead, yRead, playerXRead, playerYRead);
                        TryRead(index);
                        IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                    }
                }
            });
        }

        {
            var random = new Random(RandomSeed);
            int xRead = 0;
            int yRead = 0;
            int playerXRead = 0;
            int playerYRead = 0;

            for (int i = 0; i < Reads; ++i)
            {
                int key = GetRandomKeyArray(random);
                if (random.NextDouble() < NonSequentialReads)
                {
                    TryRead(key);
                }
                else
                {
                    int index = GetFlatArrayIndex(xRead, yRead, playerXRead, playerYRead);
                    TryRead(index);
                    IncrementRead(ref xRead, ref yRead, ref playerXRead, ref playerYRead);
                }
            }
        }

        Task.WaitAll(tasks);
    }

    // This is the magic that makes ArrayFlatDirectRandomBits fast
    private static int GetRandomKeyArray(Random random)
    {
        var key = random.Next(PlaneOffsets.Length) | random.Next(PlaneOffsets.Length) << 2 |
            random.Next(PlayerPositions.Length) << 4 | random.Next(PlayerPositions.Length) << 6;
        return key;
    }

    private static int GetRandomKeyDirect(Random random)
    {
        var key = PlaneOffsets[random.Next(PlaneOffsets.Length)] | PlaneOffsets[random.Next(PlaneOffsets.Length)] << 8 |
            PlayerPositions[random.Next(PlayerPositions.Length)] << 16 |
            PlayerPositions[random.Next(PlayerPositions.Length)] << 24;
        return key;
    }

    private static (int X, int Y, int PlayerX, int PlayerY) GetRandomIndexes(Random random)
    {
        return (random.Next(PlaneOffsets.Length),
            random.Next(PlaneOffsets.Length),
            random.Next(PlayerPositions.Length),
            random.Next(PlayerPositions.Length));
    }

    private static int GetRandomKey(Random random)
    {
        var key = GetWorldShiftKey(PlaneOffsets[random.Next(PlaneOffsets.Length)],
            PlaneOffsets[random.Next(PlaneOffsets.Length)],
            PlayerPositions[random.Next(PlayerPositions.Length)],
            PlayerPositions[random.Next(PlayerPositions.Length)]);
        return key;
    }

    private static int GetIndexKey(int xRead, int yRead, int playerXRead, int playerYRead)
    {
        return GetWorldShiftKey(PlaneOffsets[xRead], PlaneOffsets[yRead], PlayerPositions[playerXRead],
            PlayerPositions[playerYRead]);
    }

    private static void IncrementRead(ref int xRead, ref int yRead, ref int playerXRead, ref int playerYRead)
    {
        if (++xRead >= PlaneOffsets.Length)
        {
            xRead = 0;
            ++yRead;
        }

        if (yRead >= PlaneOffsets.Length)
        {
            yRead = 0;
            ++playerXRead;
        }

        if (playerXRead >= PlayerPositions.Length)
        {
            playerXRead = 0;
            ++playerYRead;
        }

        if (playerYRead >= PlayerPositions.Length)
        {
            // Wrapped around, reset
            xRead = 0;
            yRead = 0;
            playerXRead = 0;
            playerYRead = 0;
        }
    }

    // Copied code from CompoundCloud plane for the benchmark
    private static Dictionary<int, Vector2> PrecalculateWorldShiftVectors()
    {
        var shiftCache = new Dictionary<int, Vector2>(81);
        int worldShift = CLOUD_SIZE / CLOUD_PLANE_SQUARES_PER_SIDE * cloudResolution;

        foreach (int x0 in PlaneOffsets)
        {
            foreach (int y0 in PlaneOffsets)
            {
                foreach (int playerX in PlayerPositions)
                {
                    foreach (int playerY in PlayerPositions)
                    {
                        int xShift = GetEdgeShift(x0, playerX);
                        int yShift = GetEdgeShift(y0, playerY);

                        var wholePlaneShift = new Vector2(worldShift * ((4 - playerX) % 3 - 1) - CLOUD_SIZE,
                            worldShift * ((4 - playerY) % 3 - 1) - CLOUD_SIZE);

                        var edgePlanesShift = new Vector2(xShift * worldShift, yShift * worldShift);

                        int key = GetWorldShiftKey(x0, y0, playerX, playerY);
                        shiftCache[key] = wholePlaneShift + edgePlanesShift;
                    }
                }
            }
        }

        if (shiftCache.Count != 81)
            throw new Exception("Logic error in PrecalculateWorldShiftVectors");

        return shiftCache;
    }

    private static int GetEdgeShift(int coord, int playerPos)
    {
        if (coord == 0 && playerPos == 1)
            return 3;
        if (coord == 2 * CLOUD_SIZE / CLOUD_PLANE_SQUARES_PER_SIDE && playerPos == 2)
            return -3;

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 CalculateShift(int x0, int y0, int playerX, int playerY)
    {
        int worldShift = CLOUD_SIZE / CLOUD_PLANE_SQUARES_PER_SIDE * cloudResolution;

        // Reimplementation of the individual values in PrecalculateWorldShiftVectors
        int xShift = GetEdgeShift(x0, playerX);
        int yShift = GetEdgeShift(y0, playerY);

        var wholePlaneShift = new Vector2(worldShift * ((4 - playerX) % 3 - 1) - CLOUD_SIZE,
            worldShift * ((4 - playerY) % 3 - 1) - CLOUD_SIZE);

        var edgePlanesShift = new Vector2(xShift * worldShift, yShift * worldShift);
        return wholePlaneShift + edgePlanesShift;
    }

    // Seems like aggressive inlining hurts on these (in specific benchmarks)
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetWorldShiftKey(int x0, int y0, int playerX, int playerY)
    {
        // This is safe as long as the values are max 8 bit long, otherwise they will collide
        return x0 | y0 << 8 | playerX << 16 | playerY << 24;
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetFlatArrayIndex(int x0, int y0, int playerX, int playerY)
    {
        // This only works when only a few bits in each value are used, any overflow is not checked for efficiency
        // reasons here.
        // Each value gets only 2 bits as values are up to 3 distinct values per item

        // Debug code which is slow (enable only when needed to debug)
        /*if (x0 > 2 || y0 > 2 || playerX > 2 || playerY > 2)
            throw new Exception("Logic error in GetFlatArrayIndex");*/

        return x0 | y0 << 2 | playerX << 4 | playerY << 6;
    }

    private Vector2[] CalculateArrayCache()
    {
        // Our keys fit in 256 bits, so we can use a flat array for the backing store.
        var array = new Vector2[256];

        int worldShift = CLOUD_SIZE / CLOUD_PLANE_SQUARES_PER_SIDE * cloudResolution;

        for (int xIndex = 0; xIndex < PlaneOffsets.Length; ++xIndex)
        {
            var x0 = PlaneOffsets[xIndex];

            for (int yIndex = 0; yIndex < PlaneOffsets.Length; ++yIndex)
            {
                var y0 = PlaneOffsets[yIndex];

                for (int playerXIndex = 0; playerXIndex < PlayerPositions.Length; ++playerXIndex)
                {
                    var playerX = PlayerPositions[playerXIndex];

                    for (int playerYIndex = 0; playerYIndex < PlayerPositions.Length; ++playerYIndex)
                    {
                        var playerY = PlayerPositions[playerYIndex];

                        int xShift = GetEdgeShift(x0, playerX);
                        int yShift = GetEdgeShift(y0, playerY);

                        var wholePlaneShift = new Vector2(worldShift * ((4 - playerX) % 3 - 1) - CLOUD_SIZE,
                            worldShift * ((4 - playerY) % 3 - 1) - CLOUD_SIZE);

                        var edgePlanesShift = new Vector2(xShift * worldShift, yShift * worldShift);

                        int key = GetFlatArrayIndex(xIndex, yIndex, playerXIndex, playerYIndex);
                        array[key] = wholePlaneShift + edgePlanesShift;
                    }
                }
            }
        }

        return array;
    }
}
