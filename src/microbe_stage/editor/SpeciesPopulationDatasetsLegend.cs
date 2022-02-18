﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   Extension of LineChart's default datasets dropdown legend to allow sectioning of extinct species.
/// </summary>
public class SpeciesPopulationDatasetsLegend : LineChart.DataSetsDropdownLegend
{
    private List<KeyValuePair<string, ChartDataSet>> extinctSpecies;

    public SpeciesPopulationDatasetsLegend(List<KeyValuePair<string, ChartDataSet>> extinctSpecies, LineChart chart)
        : base(chart)
    {
        this.extinctSpecies = extinctSpecies;
    }

    public override Control CreateLegend(Dictionary<string, ChartDataSet> datasets, string? title)
    {
        var result = (CustomDropDown)base.CreateLegend(datasets, title);

        foreach (var species in extinctSpecies)
            AddSpeciesToList(species, TranslationServer.Translate("EXTINCT_SPECIES"));

        result.CreateElements();

        return result;
    }

    public override object Clone()
    {
        return new SpeciesPopulationDatasetsLegend(extinctSpecies, chart);
    }
}
