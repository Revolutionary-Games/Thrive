/// <summary>
///   Sate of a subscribed (and potentially locally installed) workshop item
/// </summary>
public class WorkshopItemState
{
    /// <summary>
    ///   No info tracked on the client
    /// </summary>
    public bool Untracked { get; set; }

    public bool Subscribed { get; set; }
    public bool Installed { get; set; }
    public bool NeedsUpdate { get; set; }
    public bool Downloading { get; set; }
    public bool DownloadPending { get; set; }
}
