using System;
using Newtonsoft.Json;

/// <summary>
///   The info each mod's info JSON needs to contain
/// </summary>
public class ModInfo
{
    /// <summary>
    ///   Name of the mod. Needs to be user readable. Should be unique.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; }

    /// <summary>
    ///   Author of the mod
    /// </summary>
    [JsonRequired]
    public string Author { get; set; }

    /// <summary>
    ///   The version of the mod. No restriction on format except it shouldn't be super long.
    /// </summary>
    [JsonRequired]
    public string Version { get; set; }

    /// <summary>
    ///   Description of the mod. Should be short.
    /// </summary>
    [JsonRequired]
    public string Description { get; set; }

    /// <summary>
    ///   Extended description of the mod
    /// </summary>
    public string LongDescription { get; set; }

    /// <summary>
    ///   Icon for the mod. Should be outside the mod's pck file to show in the mod loader GUI
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    ///   Optional URL to specify a site where info regarding this mod can be found. For example Github link to the
    ///   mod's source code.
    /// </summary>
    public Uri InfoUrl { get; set; }

    /// <summary>
    ///   The license the mod is licensed under. Recommended licenses are: MIT, LGPL, proprietary
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that any mods licensed under GPL or another similar infective license are incompatible to be loaded
    ///     into versions of Thrive that are not GPL licensed (for example the Steam release)
    ///   </para>
    /// </remarks>
    public string License { get; set; }

    /// <summary>
    ///   This is shown as the version of Thrive this mod version is recommended to be used with
    /// </summary>
    [JsonRequired]
    public string RecommendedThriveVersion { get; set; }

    /// <summary>
    ///   The mod will refuse to be loaded if current game version is lower than this version
    /// </summary>
    [JsonRequired]
    public string MinimumThriveVersion { get; set; }

    /// <summary>
    ///   The mod will refuse to be loaded if current game version is higher than this version
    /// </summary>
    public string MaximumThriveVersion { get; set; }

    // Start of technical properties

    /// <summary>
    ///   Specifies the relative path (from mod root folder) to a .pck file to load when enabling the mod
    /// </summary>
    public string PckToLoad { get; set; }

    /// <summary>
    ///   If set needs to point to a C# compiled DLL file that. Needs to be outside any .pck files
    /// </summary>
    public string ModAssembly { get; set; }

    /// <summary>
    ///   If ModAssembly is set this needs to be a unique class name contained in ModAssembly (meaning that it must
    ///   be named differently than inbuilt Thrive classes and other mods) that inherits IMod interface. This will be
    ///   the entrypoint to executing code in the mod's assembly.
    /// </summary>
    public string AssemblyModClass { get; set; }
}
