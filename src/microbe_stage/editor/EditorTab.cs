public enum EditorTab
{
    Report,
    PatchMap,

    /// <summary>
    ///   The main editor tab. For multicellular this is actually the body plan editor view and
    ///   <see cref="CellTypeEditor"/> is actually the way to access the cell editing.
    /// </summary>
    CellEditor,

    CellTypeEditor,
}
