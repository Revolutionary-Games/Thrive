using System;

/// <summary>
///   Event args that consist of a list of other event args
/// </summary>
public class AggregateEventArgs : EventArgs
{
    public AggregateEventArgs(params EventArgs[] args)
    {
        Args = args;
    }

    public EventArgs[] Args { get; }
}
