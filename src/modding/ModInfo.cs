using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using static FullModDetails;

/// <summary>
///   The info each mod's info JSON needs to contain
/// </summary>
public class ModInfo
{
    /// <summary>
    ///   Name of the mod. Needs to be user readable. Should be unique.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = null!;

    /// <summary>
    ///   Internal name of the mod. Needs to be unique. Shouldn't contain spaces and should match the name of the
    ///   folder the mod is distributed in.
    /// </summary>
    [JsonRequired]
    public string InternalName { get; set; } = null!;

    /// <summary>
    ///   Author of the mod
    /// </summary>
    [JsonRequired]
    public string Author { get; set; } = null!;

    /// <summary>
    ///   The version of the mod. No restriction on format except it shouldn't be super long.
    /// </summary>
    [JsonRequired]
    public string Version { get; set; } = null!;

    /// <summary>
    ///   Description of the mod. Should be short.
    /// </summary>
    [JsonRequired]
    public string Description { get; set; } = null!;

    /// <summary>
    ///   Extended description of the mod
    /// </summary>
    public string? LongDescription { get; set; }

    /// <summary>
    ///   Icon for the mod. Should be outside the mod's pck file to show in the mod loader GUI
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///   Preview Images for the mod. Should be outside the mod's pck file to show in the mod loader GUI
    /// </summary>
    public List<string>? PreviewImages { get; set; }

    /// <summary>
    ///   Optional URL to specify a site where info regarding this mod can be found. For example Github link to the
    ///   mod's source code.
    /// </summary>
    public Uri? InfoUrl { get; set; }

    /// <summary>
    ///   The license the mod is licensed under. Recommended licenses are: MIT, LGPL, proprietary
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that any mods licensed under GPL or another similar infective license are incompatible to be loaded
    ///     into versions of Thrive that are not GPL licensed (for example the Steam release)
    ///   </para>
    /// </remarks>
    public string? License { get; set; }

    /// <summary>
    ///   This is shown as the version of Thrive this mod version is recommended to be used with
    /// </summary>
    [JsonRequired]
    public string RecommendedThriveVersion { get; set; } = null!;

    /// <summary>
    ///   The mod will refuse to be loaded if current game version is lower than this version
    /// </summary>
    [JsonRequired]
    public string MinimumThriveVersion { get; set; } = null!;

    /// <summary>
    ///   The mod will refuse to be loaded if current game version is higher than this version
    /// </summary>
    public string? MaximumThriveVersion { get; set; }

    /// <summary>
    ///   Mods that is required for this one to load. These have to be loaded before this mod.
    /// </summary>
    public List<string>? Dependencies { get; set; }

    /// <summary>
    ///   Mods that is required for this one to load.
    ///   Like Dependencies except the load order does not matter.
    /// </summary>
    [JsonProperty("Required")]
    public List<string>? RequiredMods { get; set; }

    /// <summary>
    ///   Mods that can not be loaded with this one.
    /// </summary>
    [JsonProperty("Incompatible")]
    public List<string>? IncompatibleMods { get; set; }

    /// <summary>
    ///   Mods that have to be loaded before this one. Different from dependencies as it won't error if not included.
    /// </summary>
    [JsonProperty("LoadedBeforeThis")]
    public List<string>? LoadBefore { get; set; }

    /// <summary>
    ///   Mods that have to be loaded after this one but won't error if not included.
    /// </summary>
    [JsonProperty("LoadedAfterThis")]
    public List<string>? LoadAfter { get; set; }

    // Start of technical properties

    /// <summary>
    ///   Specifies the relative path (from mod root folder) to a .pck file to load when enabling the mod
    /// </summary>
    public string? PckToLoad { get; set; }

    /// <summary>
    ///   If set needs to point to a C# compiled DLL file that can be loaded. Needs to be outside any .pck files
    /// </summary>
    public string? ModAssembly { get; set; }

    /// <summary>
    ///   If ModAssembly is set this needs to be a unique class name contained in ModAssembly (meaning that it must
    ///   be named differently than inbuilt Thrive classes and other mods) that inherits IMod interface. This will be
    ///   the entrypoint to executing code in the mod's assembly.
    /// </summary>
    public string? AssemblyModClass { get; set; }

    /// <summary>
    ///   Alternative to specifying <see cref="AssemblyModClass"/>. If this is true, then the assembly is assumed to
    ///   contain only Harmony patches which will be automatically loaded and unloaded when the mod is initialized
    ///   and shutdown.
    /// </summary>
    public bool? UseAutoHarmony { get; set; }

    /// <summary>
    ///   If true the mod specifies that the game needs to be restarted for the mod to properly load / unload
    /// </summary>
    public bool RequiresRestart { get; set; }

    public VersionCompatibility GetVersionCompatibility()
    {
        var isVersionAboveMin = false;
        var isVersionBelowMax = false;

        var isVersionMinDefined = !string.IsNullOrEmpty(MinimumThriveVersion);
        var isVersionMaxDefined = !string.IsNullOrEmpty(MaximumThriveVersion);

        if (isVersionMinDefined)
        {
            isVersionAboveMin = VersionUtils.Compare(Constants.Version, MinimumThriveVersion) >= 0;
        }

        if (isVersionMaxDefined)
        {
            isVersionBelowMax = VersionUtils.Compare(Constants.Version, MaximumThriveVersion ?? string.Empty) <= 0;
        }

        if ((isVersionAboveMin && isVersionMinDefined) || (isVersionBelowMax && isVersionMaxDefined))
        {
            return VersionCompatibility.Compatible;
        }

        if (!isVersionMinDefined && !isVersionMaxDefined)
        {
            return VersionCompatibility.NotExplicitlyCompatible;
        }

        return VersionCompatibility.Incompatible;
    }
}
