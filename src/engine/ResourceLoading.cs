public static class ResourceLoading
{
    /// <summary>
    ///   Lock to hold while loading / modifying Image resources in a backgrounds thread.
    ///   Godot documentation seems to imply that it should be safe without a lock
    ///   https://docs.godotengine.org/en/stable/tutorials/threads/thread_safe_apis.html#resources but have issues
    ///   hence this lock: https://github.com/godotengine/godot/issues/55528
    ///   https://github.com/Revolutionary-Games/Thrive/issues/2078
    /// </summary>
    public static readonly object ImageLoadingLock = new();
}
