using System;
using System.Collections;

internal interface IArchiveLayoutInitializer
{
    /// <summary>
    ///   Only call when deserializing from an archive. This overrides the list contents with the actual data.
    /// </summary>
    public void InitializeFromArchive(IList data, Delegate? added, Delegate? removed);
}
