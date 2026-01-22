using SharedBase.Archive;

public abstract class EditorCombinableActionData : CombinableActionData
{
    public const ushort SERIALIZATION_VERSION_EDITOR = 2;

    public override void WriteToArchive(ISArchiveWriter writer)
    {
    }

    protected virtual void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_EDITOR or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_EDITOR);

        // No longer used
        if (version < 2)
            reader.ReadFloat();
    }
}

public abstract class EditorCombinableActionData<TContext> : EditorCombinableActionData
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_CONTEXT = 1;

    /// <summary>
    ///   The optional context this action was performed in. This is additional data in addition to the action target.
    ///   Not all editors use context info.
    /// </summary>
    public TContext? Context { get; set; }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION_EDITOR);
        base.WriteToArchive(writer);

        writer.WriteObjectOrNull(Context);
    }

    public override bool WantsToMergeWith(CombinableActionData other)
    {
        // If the other action was performed in a different context, we can't combine with it
        if (other is EditorCombinableActionData<TContext> editorActionData)
        {
            // Null context is the same as another null context, but any existing context value doesn't equal null
            if ((Context is not null && editorActionData.Context is null) ||
                (Context is null && editorActionData.Context is not null))
            {
                return false;
            }

            if (Context is not null && !Context.Equals(editorActionData.Context))
            {
                return false;
            }
        }

        return base.WantsToMergeWith(other);
    }

    public bool MatchesContext(EditorCombinableActionData<TContext> other)
    {
        return MatchesContext(Context, other);
    }

    protected static bool MatchesContext(TContext? originalContext, EditorCombinableActionData<TContext> other)
    {
        if (originalContext is null)
            return other.Context is null;

        if (other.Context is null)
            return false;

        return originalContext.Equals(other.Context);
    }

    protected override void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_CONTEXT or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_CONTEXT);

        // The base version is different (which we saved so we can read it here)
        base.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        Context = reader.ReadObjectOrNull<TContext>();
    }
}
