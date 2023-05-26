using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows the info and controls for a single city
/// </summary>
public class CityScreen : CustomWindow
{
    [Export]
    public NodePath? ShortStatsLabelPath;

    [Export]
    public NodePath AvailableBuildingsContainerPath = null!;

    [Export]
    public NodePath ConstructedBuildingsContainerPath = null!;

    [Export]
    public NodePath BuildQueueContainerPath = null!;

    private readonly List<BuildQueueItemGUI> activeBuildQueueItems = new();

#pragma warning disable CA2213
    private Label shortStatsLabel = null!;

    private Container availableBuildingsContainer = null!;

    private Container constructedBuildingsContainer = null!;

    private Container buildQueueContainer = null!;

    private PackedScene queueItemScene = null!;
    private PackedScene availableConstructionItemScene = null!;
#pragma warning restore CA2213

    private ChildObjectCache<ICityConstructionProject, AvailableConstructionProjectItem>
        createdConstructionProjectButtons = null!;

    private PlacedCity? managedCity;

    private float elapsed = 1;

    public override void _Ready()
    {
        base._Ready();

        shortStatsLabel = GetNode<Label>(ShortStatsLabelPath);

        availableBuildingsContainer = GetNode<Container>(AvailableBuildingsContainerPath);

        constructedBuildingsContainer = GetNode<Container>(ConstructedBuildingsContainerPath);

        buildQueueContainer = GetNode<Container>(BuildQueueContainerPath);

        queueItemScene = GD.Load<PackedScene>("res://src/industrial_stage/gui/BuildQueueItemGUI.tscn");
        availableConstructionItemScene =
            GD.Load<PackedScene>("res://src/industrial_stage/gui/AvailableConstructionProjectItem.tscn");

        createdConstructionProjectButtons =
            new ChildObjectCache<ICityConstructionProject, AvailableConstructionProjectItem>(
                availableBuildingsContainer, CreateAvailableConstructionItem);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible || managedCity == null)
            return;

        elapsed += delta;

        if (elapsed > Constants.CITY_SCREEN_UPDATE_INTERVAL)
        {
            elapsed = 0;

            UpdateAllCityInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a city
    /// </summary>
    /// <param name="city">The city to open this for</param>
    public void ShowForCity(PlacedCity city)
    {
        if (Visible)
        {
            Close();
        }

        managedCity = city;
        elapsed = 1;

        UpdateAllCityInfo();
        Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ShortStatsLabelPath != null)
            {
                ShortStatsLabelPath.Dispose();
                AvailableBuildingsContainerPath.Dispose();
                ConstructedBuildingsContainerPath.Dispose();
                BuildQueueContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateAllCityInfo()
    {
        UpdateCityStats();
        UpdateAvailableBuildings();
        UpdateBuildQueue();
        UpdateConstructedBuildings();
    }

    private void UpdateCityStats()
    {
        WindowTitle = managedCity!.CityName;

        // TODO: research speed, see the TODO in PlacedCity.ProcessResearch
        float researchSpeed = -1;

        var foodBalance = managedCity.CalculateFoodProduction() - managedCity.CalculateFoodConsumption();

        // Update the bottom stats bar
        shortStatsLabel.Text = TranslationServer.Translate("CITY_SHORT_STATISTICS")
            .FormatSafe(StringUtils.ThreeDigitFormat(managedCity.Population),
                StringUtils.FormatPositiveWithLeadingPlus(StringUtils.ThreeDigitFormat(foodBalance), foodBalance),
                researchSpeed);
    }

    private void UpdateAvailableBuildings()
    {
        if (managedCity == null)
            throw new InvalidOperationException("City to manage not set");

        createdConstructionProjectButtons.UnMarkAll();

        foreach (var constructionProject in managedCity.GetAvailableConstructionProjects())
        {
            var item = createdConstructionProjectButtons.GetChild(constructionProject);

            // TODO: show what is missing?
            item.Disabled = !managedCity.CanStartConstruction(constructionProject);
        }

        createdConstructionProjectButtons.DeleteUnmarked();
    }

    private void UpdateBuildQueue()
    {
        if (managedCity == null)
            throw new InvalidOperationException("City to manage not set");

        int usedIndex = 0;

        foreach (var buildQueueItemData in managedCity.GetBuildQueue())
        {
            BuildQueueItemGUI itemGUI;

            if (usedIndex >= activeBuildQueueItems.Count)
            {
                itemGUI = queueItemScene.Instance<BuildQueueItemGUI>();
                buildQueueContainer.AddChild(itemGUI);
                activeBuildQueueItems.Add(itemGUI);
            }
            else
            {
                itemGUI = activeBuildQueueItems[usedIndex];
            }

            itemGUI.Display(buildQueueItemData);

            ++usedIndex;
        }

        // Delete excess items that should not show anything
        while (usedIndex < activeBuildQueueItems.Count)
        {
            var lastIndex = activeBuildQueueItems.Count - 1;
            activeBuildQueueItems[lastIndex].QueueFree();
            activeBuildQueueItems.RemoveAt(lastIndex);
        }
    }

    private void UpdateConstructedBuildings()
    {
        constructedBuildingsContainer.QueueFreeChildren();

        // TODO: update this (for now there's no buildings to build in cities so this is not done)
    }

    private AvailableConstructionProjectItem CreateAvailableConstructionItem(ICityConstructionProject project)
    {
        var item = availableConstructionItemScene.Instance<AvailableConstructionProjectItem>();
        item.ConstructionProject = project;
        item.OnItemSelectedHandler += OnConstructionSelected;

        return item;
    }

    private void OnConstructionSelected(ICityConstructionProject project)
    {
        if (managedCity == null)
            throw new InvalidOperationException("City to manage not set");

        if (!managedCity.StartConstruction(project))
        {
            GD.Print("TODO: show the build start failure");
        }

        // Immediately update state on the next frame to show the updated conditions
        elapsed += 1;
    }
}
