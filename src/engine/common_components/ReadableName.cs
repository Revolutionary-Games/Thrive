namespace Components
{
    /// <summary>
    ///   Player readable name for an entity. Must be set on init so always use the constructor.
    /// </summary>
    public struct ReadableName
    {
        public LocalizedString Name;

        public ReadableName(LocalizedString name)
        {
            Name = name;
        }
    }
}
