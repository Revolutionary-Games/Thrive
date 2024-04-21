using System;

/// <summary>
///   Thrown when trying to access a static instance that has not been loaded yet
/// </summary>
[Serializable]
public class InstanceNotLoadedYetException : InvalidOperationException
{
    public InstanceNotLoadedYetException() : base(
        $"Instance not loaded yet, called from:\n{Environment.StackTrace}\nend of not loaded yet stacktrace")
    {
    }
}
