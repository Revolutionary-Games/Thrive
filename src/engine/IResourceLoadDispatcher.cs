using System;

internal interface IResourceLoadDispatcher
{
    void ExecuteFullLoad(IResource resource);

    void InvokeCompletionCallback(IResource resource);
}

internal interface IResourceLoadCompletionUnit
{
    double EstimatedDurationSeconds { get; }

    void Execute();
}

internal interface IResourceLoadFrameTimeSource
{
    TimeSpan Elapsed { get; }
}
