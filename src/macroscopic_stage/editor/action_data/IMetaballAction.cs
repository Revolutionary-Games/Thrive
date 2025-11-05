using SharedBase.Archive;

public interface IMetaballAction : IArchivable
{
    /// <summary>
    ///   Called when the serializer is done calling the constructor and non-constructor base type properties need
    ///   reading
    /// </summary>
    public void FinishBaseLoad(ISArchiveReader reader, ushort version);
}
