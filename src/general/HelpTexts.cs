using System.Collections.Generic;

/// <summary>
///   Help texts from json.
/// </summary>
public class HelpTexts : IRegistryType
{
    public List<HelpText> Messages;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Messages == null || Messages.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing help messages");
    }

    public void ApplyTranslations()
    {
    }
}

/// <summary>
///   A single help message.
/// </summary>
public class HelpText
{
    /// <summary>
    ///   Sides specifying which side the help text should be displayed in the help screen.
    /// </summary>
    public enum TextColumn
    {
        None,
        Left,
        Right,
    }

    public TextColumn Column { get; set; } = TextColumn.None;

    public string Message { get; set; }
}
