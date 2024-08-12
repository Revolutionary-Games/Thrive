using System.Collections.Generic;
using Godot;

/// <summary>
///   Utility for universally accessible Thriveopedia actions, such as opening a page by its name.
/// </summary>
public class ThriveopediaManager
{
    private static readonly ThriveopediaManager ManagerInstance = new();

    private readonly List<Thriveopedia> activeThriveopedias = new();

    private readonly List<ISpeciesDataProvider> speciesDataProviders = new();

    public delegate void OnPageOpened(string pageName);

    public static ThriveopediaManager Instance => ManagerInstance;

    /// <summary>
    ///   Action to open the Thriveopedia in the current game context, e.g. from the main menu or from the pause menu.
    /// </summary>
    public OnPageOpened? OnPageOpenedHandler { get; set; }

    /// <summary>
    ///   Opens a page in the Thriveopedia via the appropriate menu context. Name must match the PageName property
    ///   of the desired page.
    /// </summary>
    public static void OpenPage(string pageName)
    {
        if (Instance.OnPageOpenedHandler == null)
        {
            GD.PrintErr($"Attempted to open page {pageName} before Thriveopedia was initialised");
            return;
        }

        Instance.OnPageOpenedHandler(pageName);
    }

    public static Species? GetActiveSpeciesData(uint speciesId)
    {
        foreach (var provider in Instance.speciesDataProviders)
        {
            var species = provider.GetActiveSpeciesData(speciesId);

            if (species != null)
                return species;
        }

        return null;
    }

    /// <summary>
    ///   Report a non-Thriveopedia SpeciesDataProvider
    /// </summary>
    public static void ReportNonThriveopediaSpeciesDataProvider(ISpeciesDataProvider speciesDataProvider)
    {
        if (speciesDataProvider is Thriveopedia)
        {
            GD.PrintErr("Thriveopedia registered as generic species data provider");
            return;
        }

        if (Instance.speciesDataProviders.Contains(speciesDataProvider))
        {
            GD.PrintErr("Duplicate species data provider registration");
            return;
        }

        Instance.speciesDataProviders.Add(speciesDataProvider);
    }

    /// <summary>
    ///   Removes a non-Thriveopedia SpeciesDataProvider
    /// </summary>
    public static void RemoveNonThriveopediaSpeciesDataProvider(ISpeciesDataProvider speciesDataProvider)
    {
        if (speciesDataProvider is Thriveopedia)
        {
            GD.PrintErr("Thriveopedia removed as generic species data provider");
            return;
        }

        if (!Instance.speciesDataProviders.Remove(speciesDataProvider))
        {
            GD.PrintErr("Failed to unregister species data provider");
        }
    }

    /// <summary>
    ///   Report an active Thriveopedia instance that can respond to data requests
    /// </summary>
    public static void ReportActiveThriveopedia(Thriveopedia thriveopedia)
    {
        if (Instance.activeThriveopedias.Contains(thriveopedia))
        {
            GD.PrintErr("Duplicate Thriveopedia registration");
            return;
        }

        Instance.activeThriveopedias.Add(thriveopedia);
        Instance.speciesDataProviders.Add(thriveopedia);
    }

    public static void RemoveActiveThriveopedia(Thriveopedia thriveopedia)
    {
        if (!Instance.activeThriveopedias.Remove(thriveopedia))
        {
            GD.PrintErr("Failed to unregister Thriveopedia");
        }

        if (!Instance.speciesDataProviders.Remove(thriveopedia))
        {
            GD.PrintErr("Failed to unregister Thriveopedia as species data provider");
        }
    }
}
