using System;

/// <summary>
///   Main object for containing player changeable game settings
/// </summary>
public class Settings
{
    private static readonly Settings INSTANCE = new Settings();

    static Settings()
    {
    }

    private Settings()
    {
        // TODO: load from godot usr path file
    }

    public static Settings Instance
    {
        get
        {
            return INSTANCE;
        }
    }

    public void Save()
    {
        throw new NotImplementedException();
    }
}
