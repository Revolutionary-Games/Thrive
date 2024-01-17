namespace Components
{
    /// <summary>
    ///   Entity is a member of a species and has species related data applied to it. Note that for most things
    ///   <see cref="CellProperties"/> should be used instead as that works for early multicellular things as well.
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct MicrobeSpeciesMember
    {
        public MicrobeSpecies Species;
    }
}
