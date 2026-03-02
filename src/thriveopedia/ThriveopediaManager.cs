using System;
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
    ///   This looks up in the tree to find the owning Thriveopedia instance. This is now preferred rather than looking
    ///   through the open Thriveopedias because there is the persistent pause menu one, and it has caused various
    ///   bugs.
    /// </summary>
    /// <param name="currentNode">Node that is contained in a Thriveopedia</param>
    /// <returns>
    ///   The parent Thriveopedia or null if a node not in a Thriveopedia. If null methods need to cancel (this handles
    ///   error reporting).
    /// </returns>
    public static Thriveopedia? GetParentThriveopedia(Control currentNode)
    {
        var parent = currentNode.GetParent();

        while (parent != null)
        {
            if (parent is Thriveopedia thriveopedia)
                return thriveopedia;

            parent = parent.GetParent();
        }

        GD.PrintErr($"Thriveopedia not found when looking up from node: {currentNode.GetPath()}");
        LogInterceptor.ForwardCaughtError(new Exception("Cannot find parent Thriveopedia instance"));
        return null;
    }

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
