using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles saving cached items to disk and loading them back. Also handles maintaining the cache.
/// </summary>
/// <remarks>
///   <para>
///     It might be possible to improve disk use efficiency by only saving items to disk that are used more than once
///     (though this needs more memory cache time to ensure items have a chance of needing to be used multiple times).
///   </para>
/// </remarks>
public partial class DiskCache : Node, IComputeCache<IImageTask>
{
    private static DiskCache? instance;

    /// <summary>
    ///   Lock that must be held when working with <see cref="cacheInfo"/>
    /// </summary>
    private readonly ReaderWriterLockSlim infoLock = new();

    private readonly object generalLock = new();

    /// <summary>
    ///   Info about all the cache items, always stored in memory to be able to quickly know if something is in cache
    ///   or not.
    /// </summary>
    private readonly Dictionary<ulong, CacheItemInfo> cacheInfo = new();

    private readonly List<ulong> cacheItemsToRemove = new();

    private readonly ConcurrentQueue<CacheItemInfo> objectsPendingSave = new();
    private readonly ConcurrentQueue<CacheItemInfo> objectsPendingLoad = new();
    private readonly ConcurrentQueue<string> deleteQueue = new();

    private readonly Stack<CacheItemInfo> unusedCacheInfoObjects = new();

    private readonly byte[] pathDecodeTempMemory = new byte[128];

    /// <summary>
    ///   Temporary memory used to generate cache paths. Can only be used while <see cref="infoLock"/> is held in write
    ///   mode.
    /// </summary>
    private readonly StringBuilder itemPathTemp = new();

    // Variables for CalculateCachePath
    private readonly object pathBuildLock = new();
    private readonly byte[] pathBuilderRaw = new byte[256];

    // Config
    private float cacheItemKeepTime = Constants.DISK_CACHE_DEFAULT_KEEP;
    private float cacheItemMemoryTime = Constants.MEMORY_BEFORE_DISK_CACHE_TIME;
    private long maxTotalCacheSize = Constants.DISK_CACHE_DEFAULT_MAX_SIZE;
    private int maxMemoryItems = Constants.MEMORY_PHOTO_CACHE_MAX_ITEMS;

    // Other variables
    private double currentTime;

    /// <summary>
    ///   Only full seconds part of <see cref="currentTime"/> (for faster access)
    /// </summary>
    private int currentTimeFullSeconds;

    private double timeSinceLastCheck;
    private double timeSinceLastSave;
    private double saveResumeTime;

    private long totalCacheSize;

    private long cacheMissesOrInserts;
    private long cacheHits;
    private long cacheKeyConflicts;

    private bool loadTaskRunning;
    private bool saveTaskRunning;
    private bool deleteTaskRunning;

    private bool ranFullCheck;
    private bool cacheLoaded;

    private bool fullCheckQueued;
    private bool saveQueued;

    /// <summary>
    ///   When only a few executor threads are available, some parallel operations are avoided
    /// </summary>
    private bool hasLimitedExecutors;

    public static DiskCache Instance => instance ?? throw new InstanceNotLoadedYetException();

    public long TotalCacheSize => totalCacheSize;

    public override void _Ready()
    {
        base._Ready();

        if (instance != null)
            GD.PrintErr("Multiple DiskCache instances have been created");

        instance = this;

        if (Settings.Instance.UseDiskCache.Value)
        {
            DirAccess.MakeDirAbsolute(Constants.CACHE_FOLDER);
            Invoke.Instance.QueueForObject(LoadCache, this);
        }
        else
        {
            GD.Print("Disk caching is disabled");
        }

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        // Stop pending operations after the current ones
        objectsPendingSave.Clear();
        objectsPendingLoad.Clear();

        base._ExitTree();

        // Probably don't need to try to save cache items here as it would slow down the game shutting down
        // But will wait here for the current saves and loads on the task threads to complete
        var elapsed = new Stopwatch();
        elapsed.Start();

        while (true)
        {
            lock (generalLock)
            {
                if (!saveTaskRunning && !loadTaskRunning)
                    break;
            }

            if (elapsed.Elapsed > TimeSpan.FromSeconds(10))
            {
                GD.PrintErr("Waited 10 seconds for pending cache operations to complete, " +
                    "stopping waiting as they are still not complete");
                break;
            }

            Thread.Sleep(50);
        }

        // Make sure all deletes are performed to not lose track of any items (in case in the future we have a registry
        // of what cache items exist in like a JSON format)
        while (deleteQueue.TryDequeue(out var path))
        {
            if (DirAccess.RemoveAbsolute(path) != Error.Ok)
                GD.PrintErr($"Failed to delete cache item: {path}");

            if (elapsed.Elapsed > TimeSpan.FromSeconds(15))
            {
                GD.PrintErr("Stopping trying to clean cache entries after 15 seconds of trying to shutdown");
            }
        }

        instance = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        int threads = TaskExecutor.Instance.ParallelTasks;

        if (Settings.Instance.RunAutoEvoDuringGamePlay.Value)
            --threads;

        var settings = Settings.Instance;

        // Update config that can change. Not all of these can change each frame but as there's just one cache object
        // refreshing everything each frame shouldn't be that bad.
        hasLimitedExecutors = threads < 3;
        cacheItemKeepTime = settings.DiskCacheMaxTime;
        cacheItemMemoryTime = settings.DiskMemoryCachePortionTime;
        maxTotalCacheSize = settings.DiskCacheSize;
        maxMemoryItems = settings.MemoryCacheMaxSize;

        currentTime += delta;

        currentTimeFullSeconds = (int)currentTime;

        CleanCacheIfTime(delta);

        saveResumeTime += delta;

        // Resume a save queue if there are items in it (a single run only saves a few items to make sure there aren't
        // massive lag spikes if something ends up waiting for the event)
        if (saveResumeTime >= Constants.DISK_CACHE_SAVE_RESUME_CHECK_INTERVAL)
        {
            saveResumeTime = 0;

            lock (generalLock)
            {
                if (!loadTaskRunning && !deleteTaskRunning && !objectsPendingSave.IsEmpty)
                {
                    StartSaveIfRequired();
                }
            }
        }

        // Make sure load is always running if there are waiting objects to load
        if (!objectsPendingLoad.IsEmpty)
        {
            StartLoadIfRequired();
        }
    }

    public IImageTask? Get(ulong cacheKey)
    {
        infoLock.EnterReadLock();
        try
        {
            if (cacheInfo.TryGetValue(cacheKey, out var cacheItem))
            {
                if (cacheItem.ItemType != CacheItemType.Png)
                {
                    OnCacheTypeConflict(cacheKey, cacheItem, CacheItemType.Png);
                    return null;
                }

                Interlocked.Increment(ref cacheHits);
                cacheItem.LastAccessTime = currentTimeFullSeconds;

                if (cacheItem.LoadedItem != null)
                {
                    TriggerItemLoadIfNeeded(cacheItem, cacheItem.LoadedItem);
                    return (IImageTask)cacheItem.LoadedItem;
                }

                // Need to start loading this cache item before returning the object tracking the load
                return (IImageTask)StartCacheItemLoad(cacheItem);
            }
        }
        finally
        {
            infoLock.ExitReadLock();
        }

        return null;
    }

    public void Insert(IImageTask item)
    {
        Insert(item.CalculateCacheHash(), item);
    }

    public void Insert(ulong cacheKey, IImageTask item)
    {
        // Need to calculate a path for the item that will be used when saving it
        var path = CalculateCachePath(cacheKey, CacheItemType.Png);

        // Make sure the cache path is stored in case the item is written to disk
        item.CachePath = path;

        Interlocked.Increment(ref cacheMissesOrInserts);

        infoLock.EnterWriteLock();
        try
        {
            if (cacheInfo.TryGetValue(cacheKey, out var cacheItem))
            {
                // Replacing an existing item, this is a more complicated case

                if (cacheItem.Status != CacheItemInfo.OperationStatus.None)
                {
                    // If state us not none, then the item is being processed, and it is not safe to modify so we'll
                    // need a different object here
                    GD.Print("Replacing cache item that is not in normal state, need to create a new holding object");
                    cacheItem = GetCacheItemEntryToUse(path, CacheItemType.Png);
                }
                else
                {
                    // Need to delete the previous item
                    QueueDeleteItemPath(cacheItem);
                }

                // Modify the existing object to modify the "immutable" parts of the cache entry
                cacheItem.ItemType = CacheItemType.Png;
                cacheItem.Hash = cacheKey;
            }
            else
            {
                cacheItem = GetCacheItemEntryToUse(path, CacheItemType.Png);
                cacheInfo.Add(cacheKey, cacheItem);
            }

            // Set up the new cache entry
            cacheItem.LoadedItem = item;
            cacheItem.Status = CacheItemInfo.OperationStatus.None;
            cacheItem.Path = path;
            cacheItem.LastAccessTime = currentTimeFullSeconds;
            cacheItem.WrittenToDisk = false;
            cacheItem.Size = 0;
        }
        finally
        {
            infoLock.ExitWriteLock();
        }
    }

    public void InMainMenu()
    {
        if (!ranFullCheck)
        {
            ranFullCheck = true;

            if (!Settings.Instance.UseDiskCache.Value)
            {
                GD.Print("Disk cache is disabled, will delete it if it exists");
                Clear();
                return;
            }

            if (!cacheLoaded)
            {
                fullCheckQueued = true;
                GD.Print("Disk cache not loaded yet, pruning it later");
            }
            else
            {
                PruneDiskCache();
            }
        }
    }

    public void CleanCacheIfTime(double delta)
    {
        timeSinceLastCheck += delta;
        if (timeSinceLastCheck > Constants.DISK_CACHE_CHECK_INTERVAL)
        {
            timeSinceLastCheck = 0;
            PruneDiskCache();
        }

        timeSinceLastSave += delta;
        if (timeSinceLastSave > Constants.DISK_CACHE_IDLE_SAVE_INTERVAL)
        {
            timeSinceLastSave = 0;

            RunPeriodicSmallSave();
        }
    }

    public void Clear()
    {
        GD.Print("Deleting entire on-disk cache");

        objectsPendingSave.Clear();
        objectsPendingLoad.Clear();
        deleteQueue.Clear();

        // Apparently Godot doesn't have a method to permanently delete a folder recursively
        try
        {
            if (DirAccess.DirExistsAbsolute(Constants.CACHE_IMAGES_FOLDER))
            {
                // Again, have to use standard C# here as Godot apparently doesn't have a recursive folder delete
                // method
                Directory.Delete(ProjectSettings.GlobalizePath(Constants.CACHE_IMAGES_FOLDER), true);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to delete image cache: {e}");
            return;
        }

        infoLock.EnterWriteLock();
        try
        {
            totalCacheSize = 0;

            objectsPendingLoad.Clear();

            foreach (var cacheInfoValue in cacheInfo.Values)
            {
                cacheInfoValue.LoadedItem = null;

                // Preserve some of the cache entries for later use
                if (unusedCacheInfoObjects.Count < 100)
                {
                    cacheInfoValue.Status = CacheItemInfo.OperationStatus.None;
                    unusedCacheInfoObjects.Push(cacheInfoValue);
                }
            }

            cacheInfo.Clear();
        }
        finally
        {
            infoLock.ExitWriteLock();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            infoLock.Dispose();
        }

        base.Dispose(disposing);
    }

    private void LoadCache()
    {
        // This takes a while but the cache is basically unusable until this is done, so basically required
        infoLock.EnterWriteLock();
        try
        {
            totalCacheSize = 0;
            cacheInfo.Clear();

            // TODO: if cache is from different Thrive version, should probably delete it or at least not load most of
            // the cache

            LoadImages();

            cacheLoaded = true;

            GD.Print($"Disk cache loaded, total size: {totalCacheSize / Constants.MEBIBYTE:F1} MiB");

            if (fullCheckQueued)
                PruneDiskCache();
        }
        finally
        {
            infoLock.ExitWriteLock();
        }
    }

    private void LoadImages()
    {
        if (!DirAccess.DirExistsAbsolute(Constants.CACHE_IMAGES_FOLDER))
            return;

        // I tried to make this using the default Godot file access, but it is too limited so this uses C# APIs here
        // -hhyyrylainen

        var path = ProjectSettings.GlobalizePath(Constants.CACHE_IMAGES_FOLDER);

        var now = DateTime.UtcNow;

        foreach (var entry in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
        {
            // Sadly a bunch of objects need to be created to inspect the data here, but the alternative of using
            // Godot methods doesn't even allow access to the file length so that's not usable
            var entryFileData = new FileInfo(entry);

            // We can't really know when this was last accessed so we use the modify time to make a pretty good
            // alternative to know when this entry needs to be deleted. It's taken as negative to give that long in the
            // past as the current cache time starts at 0 and increments while the game runs.
            var modifiedLast = -(now - entryFileData.LastWriteTimeUtc).TotalSeconds;

            // TODO: should this already apply cache time pruning here (and queue delete the files)?

            // Convert back to Godot path
            itemPathTemp.Append(Constants.CACHE_IMAGES_FOLDER);
            itemPathTemp.Append('/');
            itemPathTemp.Append(entry, path.Length, entry.Length - path.Length);

            var cacheEntry = GetCacheItemEntryToUse(itemPathTemp.ToString(), CacheItemType.Png);
            itemPathTemp.Clear();

            // +1 is specified here to skip the last path separator part to get purely the right part of the path
            // we want to parse
            cacheEntry.Hash = GetHashFromPathName(path.Length + 1, entry);

            // In the unlikely case that file timestamps are like 60+ years old (or in the future) use a fallback value
            if (modifiedLast is < int.MinValue or > int.MaxValue)
            {
                cacheEntry.LastAccessTime = int.MinValue;
            }
            else
            {
                cacheEntry.LastAccessTime = (int)modifiedLast;
            }

            cacheEntry.Size = entryFileData.Length;
            Interlocked.Add(ref totalCacheSize, entryFileData.Length);

            if (!entry.EndsWith(".png"))
                GD.PrintErr("Other image types in the cache aren't handled currently (other than PNG)");

            // Loaded from disk so the data is already on disk and doesn't need to be written back when removing this
            // entry from memory
            cacheEntry.WrittenToDisk = true;

            // Very important step, store the item metadata in the cache structure so that it can be found
            cacheInfo[cacheEntry.Hash] = cacheEntry;
        }
    }

    private ulong GetHashFromPathName(int prefixToSkip, string path)
    {
        return CachePaths.ParseCachePath(path, prefixToSkip, pathDecodeTempMemory, false);
    }

    /// <summary>
    ///   Calculates a cache path for where to store a cache item. Only one call can happen at once due to locking
    ///   (which is needed to share existing buffer memory)
    /// </summary>
    /// <param name="cacheKey">Cache hash</param>
    /// <param name="type">Type of item (affects the path extension)</param>
    /// <returns>The Godot-style path to store the cache item at</returns>
    private string CalculateCachePath(ulong cacheKey, CacheItemType type)
    {
        lock (pathBuildLock)
        {
            return CachePaths.GenerateCachePath(cacheKey, type, pathBuilderRaw);
        }
    }

    private ICacheItem StartCacheItemLoad(CacheItemInfo cacheItem)
    {
        cacheItem.PrepareLoad();
        objectsPendingLoad.Enqueue(cacheItem);

        StartLoadIfRequired();

        if (cacheItem.LoadedItem == null)
            throw new Exception("Incorrect logic in cache item load prepare");

        return cacheItem.LoadedItem;
    }

    private void TriggerItemLoadIfNeeded(CacheItemInfo itemInfo, ISavableCacheItem cacheItem)
    {
        if (cacheItem is ILoadableCacheItem loadableItem)
        {
            if (!loadableItem.Finished && itemInfo.Status != CacheItemInfo.OperationStatus.Loading)
            {
                // This is probably fine to do this without locking first
                itemInfo.Status = CacheItemInfo.OperationStatus.Loading;
                objectsPendingLoad.Enqueue(itemInfo);
            }
        }
    }

    /// <summary>
    ///   Gets a cache item. <see cref="infoLock"/> should be locked.
    /// </summary>
    /// <returns>An entry to use</returns>
    private CacheItemInfo GetCacheItemEntryToUse(string forPath, CacheItemType type)
    {
        if (unusedCacheInfoObjects.TryPop(out var result))
        {
#if DEBUG
            if (result.Status != CacheItemInfo.OperationStatus.None)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();

                throw new Exception("Cache has an item that was in use");
            }
#endif

            // Adjust the existing object to reuse it
            result.Path = forPath;
            result.ItemType = type;

            result.LastAccessTime = currentTimeFullSeconds;

            // Clear some state for safety to ensure incorrect data is not set
            result.Hash = 0;
            result.LoadedItem = null;
            result.WrittenToDisk = false;
            result.Size = 0;
            result.Status = CacheItemInfo.OperationStatus.None;

            return result;
        }

        return new CacheItemInfo { Path = forPath, ItemType = type };
    }

    private void PruneDiskCache()
    {
        Invoke.Instance.Perform(RunCachePruning);
    }

    private void RunCachePruning()
    {
        bool startWrites = false;

        lock (cacheItemsToRemove)
        {
            infoLock.EnterUpgradeableReadLock();
            try
            {
                // Comparisons done as ints to hopefully be slightly faster than double comparisons
                var cutoff = (int)(currentTime - cacheItemKeepTime);
                var unloadCutoff = (int)(currentTime - cacheItemMemoryTime);

                int itemsLeft = maxMemoryItems;

                foreach (var itemInfo in cacheInfo)
                {
                    var value = itemInfo.Value;
                    if (value.LastAccessTime < cutoff)
                    {
                        cacheItemsToRemove.Add(itemInfo.Key);
                    }
                    else if ((value.LastAccessTime < unloadCutoff || itemsLeft < 0) && value.LoadedItem != null)
                    {
                        // Should unload this item (and write to disk if not already), as it is either old or we have
                        // way too much loaded stuff
                        if (!value.WrittenToDisk && value.LoadedItem.Finished)
                        {
                            startWrites = true;
                            value.WrittenToDisk = true;
                            itemInfo.Value.Status = CacheItemInfo.OperationStatus.Saving;
                            objectsPendingSave.Enqueue(itemInfo.Value);
                        }
                        else if (value.LoadedItem.Finished && value.Status != CacheItemInfo.OperationStatus.Saving)
                        {
                            // Already written to disk, can just let go of item
                            if (value.LoadedItem is ILoadableCacheItem loadableItem)
                            {
                                // Just unload the main data if this is a cache item that is metadata for an on-disk
                                // resource as when being loaded again the metadata would get just re-created
                                loadableItem.Unload();
                            }
                            else
                            {
                                // Item can't be reloaded from disk, as-is, so remove reference for garbage collector
                                // to delete the item
                                value.LoadedItem = null;
                            }
                        }
                    }
                    else if (value.LoadedItem is { Finished: true } or not ILoadableCacheItem)
                    {
                        // Count in limit if currently finished loading or is of a type that cannot have parts of it be
                        // unloaded
                        --itemsLeft;
                    }
                }

                infoLock.EnterWriteLock();
                try
                {
                    foreach (var toRemove in cacheItemsToRemove)
                    {
                        if (cacheInfo.Remove(toRemove, out var value))
                        {
                            Interlocked.Add(ref totalCacheSize, -value.Size);

                            QueueDeleteItemPath(value);

                            // If the object is currently being processed, don't put it into the reuse buffer
                            if (value.Status != CacheItemInfo.OperationStatus.None)
                            {
                                if (value.LoadedItem is ILoadableCacheItem loadableItem)
                                {
                                    // This doesn't really matter for current unloadable objects but might in the
                                    // future matter for some type
                                    loadableItem.Unload();
                                }

                                // Let go of this potentially expensive object reference
                                value.LoadedItem = null;
                                unusedCacheInfoObjects.Push(value);
                            }
                        }
                        else
                        {
                            GD.PrintErr("Unable to remove item from cache");
                        }
                    }
                }
                finally
                {
                    infoLock.ExitWriteLock();
                }
            }
            finally
            {
                infoLock.ExitUpgradeableReadLock();
            }

            cacheItemsToRemove.Clear();
        }

        if (!deleteQueue.IsEmpty)
            StartDeleteIfRequired();

        if (startWrites)
            StartSaveIfRequired();
    }

    private void RunPeriodicSmallSave()
    {
        // Save a couple of items even when not needing to do anything else
        int itemsLeft = Constants.DISK_CACHE_IDLE_SAVE_ITEMS;

        bool startWrites = false;

        bool upgradedLock = false;

        infoLock.EnterUpgradeableReadLock();
        try
        {
            foreach (var itemInfo in cacheInfo)
            {
                if (!itemInfo.Value.WrittenToDisk && itemInfo.Value.LoadedItem is { Finished: true })
                {
                    if (!upgradedLock)
                    {
                        infoLock.EnterWriteLock();
                        upgradedLock = true;
                    }

                    startWrites = true;
                    itemInfo.Value.WrittenToDisk = true;
                    itemInfo.Value.Status = CacheItemInfo.OperationStatus.Saving;
                    objectsPendingSave.Enqueue(itemInfo.Value);

                    if (--itemsLeft <= 0)
                        break;
                }
            }
        }
        finally
        {
            if (upgradedLock)
            {
                infoLock.ExitWriteLock();
            }

            infoLock.ExitUpgradeableReadLock();
        }

        if (startWrites)
        {
            lock (generalLock)
            {
                if (loadTaskRunning || deleteTaskRunning)
                {
                    saveQueued = true;
                }
                else
                {
                    StartSaveIfRequired();
                }
            }
        }
    }

    private void QueueDeleteItemPath(CacheItemInfo value)
    {
        deleteQueue.Enqueue(value.Path);

        // Keep estimate of cache size on disk accurate
        Interlocked.Add(ref totalCacheSize, -value.Size);
        value.Size = 0;
    }

    private void StartSaveIfRequired()
    {
        lock (generalLock)
        {
            if (saveTaskRunning)
                return;

            // Limiting if working with just a few background threads
            if (hasLimitedExecutors && loadTaskRunning)
            {
                saveQueued = true;
            }
            else
            {
                saveTaskRunning = true;
                Invoke.Instance.Perform(RunSaveQueue);
            }
        }
    }

    private void StartLoadIfRequired()
    {
        lock (generalLock)
        {
            if (loadTaskRunning)
                return;

            loadTaskRunning = true;
            Invoke.Instance.Perform(RunLoadQueue);
        }
    }

    private void StartDeleteIfRequired()
    {
        lock (generalLock)
        {
            if (deleteTaskRunning)
                return;

            deleteTaskRunning = true;
            Invoke.Instance.Perform(RunDeleteQueue);
        }
    }

    private void RunLoadQueue()
    {
        while (objectsPendingLoad.TryDequeue(out var item))
        {
            try
            {
                item.Load();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to load item from cache: {item.Path}, due to: {e}, will delete it");

                infoLock.EnterWriteLock();
                try
                {
                    if (cacheInfo.Remove(item.Hash))
                    {
                        Interlocked.Add(ref totalCacheSize, -item.Size);

                        // We rely on the occasional triggering of the delete queue so we don't need to trigger one
                        // here
                        QueueDeleteItemPath(item);

                        // This loses the object and is not able to reuse it, but as this is a rare error handling
                        // condition, that shouldn't be serious here
                    }
                    else
                    {
                        GD.PrintErr("Unable to remove failed item from cache");
                    }
                }
                finally
                {
                    infoLock.ExitWriteLock();
                }
            }
        }

        lock (generalLock)
        {
            loadTaskRunning = false;

            // If a task was skipped due to constraints on running, start it now
            if (saveQueued)
            {
                StartSaveIfRequired();
            }
        }
    }

    private void RunSaveQueue()
    {
        saveQueued = false;

        int saved = 0;

        while (objectsPendingSave.TryDequeue(out var item))
        {
            ++saved;

            try
            {
                item.Save();

                // Item is now saved, so it is now known how big the item is, so keep track of the total size
                // This if is here to make sure if an item is saved twice, it doesn't get counted twice in the total
                // size
                if (item.Size < 1)
                {
                    var fileInfo = new FileInfo(ProjectSettings.GlobalizePath(item.Path));
                    item.Size = fileInfo.Length;
                    Interlocked.Add(ref totalCacheSize, item.Size);
                }
            }
            catch (Exception e)
            {
                item.WrittenToDisk = false;
                GD.PrintErr($"Error when writing cache item to disk ({item.Path}): {e}");
            }

            // Limit how many disk writes are done quickly, a new save task is queued by _PhysicsProcess
            if (saved >= Constants.DISK_CACHE_SAVES_PER_RUN)
            {
                break;
            }
        }

        lock (generalLock)
        {
            saveTaskRunning = false;
        }
    }

    private void RunDeleteQueue()
    {
        while (deleteQueue.TryDequeue(out var path))
        {
            var error = DirAccess.RemoveAbsolute(path);
            if (error != Error.Ok)
            {
                // File not existing already is not an error in case duplicate deletes are triggered
                if (error != Error.DoesNotExist)
                    GD.PrintErr($"Failed to delete cache item: {path}");
            }
        }

        lock (generalLock)
        {
            deleteTaskRunning = false;
        }
    }

    private void OnCacheTypeConflict(ulong key, CacheItemInfo item, CacheItemType wantedType)
    {
        GD.PrintErr($"Conflict between different types of cache items! Wanted: {wantedType}, actual: " +
            $"{item.ItemType}, hash: {key}");

        Interlocked.Increment(ref cacheKeyConflicts);

        // This doesn't actually need to delete the item as when it is inserted into the cache, that is when the
        // deletion happens (when the old item is evicted)
    }

    private class CacheItemInfo
    {
        public required string Path;
        public ulong Hash;
        public long Size;

        /// <summary>
        ///   Last access of this item since the start of the game process. This should be way more than enough for
        ///   anything as this is in seconds (so over 60 years until rollover)
        /// </summary>
        public int LastAccessTime;

        [JsonIgnore]
        public ISavableCacheItem? LoadedItem;

        public CacheItemType ItemType;

        public OperationStatus Status;

        /// <summary>
        ///   When false a cache item is pending a write to disk
        /// </summary>
        [JsonIgnore]
        public bool WrittenToDisk;

        public enum OperationStatus
        {
            None = 0,
            Loading,
            Saving,
        }

        public void PrepareLoad()
        {
#if DEBUG
            if (Status != OperationStatus.None)
                throw new InvalidOperationException($"Incorrect status for cache item in prepare load: {Status}");
#endif

            Status = OperationStatus.Loading;

            // Create the cache data item in unloaded state so that it can be already returned to the code that wants
            // to wait for the cache load
            switch (ItemType)
            {
                case CacheItemType.Png:
                    LoadedItem = new CacheLoadedImage(Hash, Path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void Load()
        {
#if DEBUG
            if (Status != OperationStatus.Loading)
                throw new InvalidOperationException($"Incorrect status for cache item in load: {Status}");
#endif

            var data = LoadedItem;

            if (data == null)
            {
                GD.PrintErr("Cache item has no data object to perform load into. Will try to recover...");

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif

                Status = OperationStatus.None;
                PrepareLoad();
                Load();
                return;
            }

            if (data is ILoadableCacheItem loadable)
            {
                loadable.Load();
            }
            else
            {
                throw new NotSupportedException(
                    $"Unknown type of loadable cache item, cannot load it ({data.GetType()}");
            }

            // Loaded from disk, so no need to write this back if purging from memory
            WrittenToDisk = true;
            Status = OperationStatus.None;
        }

        internal void Save()
        {
#if DEBUG
            if (Status != OperationStatus.Saving)
                throw new InvalidOperationException($"Incorrect status for cache item in save: {Status}");
#endif

            var data = LoadedItem;

            if (data == null)
            {
                GD.PrintErr("Cache item has no data object to save to disk");
            }
            else
            {
                data.Save();
            }

            WrittenToDisk = true;
            Status = OperationStatus.None;
        }
    }
}
