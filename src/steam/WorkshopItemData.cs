using System.Collections.Generic;

public class WorkshopItemData
{
    public WorkshopItemData(ulong id, string title, string contentFolder, string previewImagePath)
    {
        Id = id;
        Title = title;
        ContentFolder = contentFolder;
        PreviewImagePath = previewImagePath;
    }

    public ulong Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public SteamItemVisibility Visibility { get; set; } = SteamItemVisibility.Public;
    public string ContentFolder { get; set; }
    public string PreviewImagePath { get; set; }
    public List<string> Tags { get; set; } = new();
}
