public class SteamUploadProgress
{
    public bool ErrorHappened { get; set; }

    public ulong ProcessedBytes { get; set; }

    public ulong TotalBytes { get; set; }
}
