namespace Components
{
    /// <summary>
    ///   Player readable name for an entity. Should be set on init, if stays null a fallback error name is used
    /// </summary>
    public struct ReadableName
    {
        public LocalizedString? Name;
    }
}
