﻿using Godot;

/// <summary>
///   A page in the Thriveopedia containing information about a stage.
/// </summary>
public partial class ThriveopediaStagePage : ThriveopediaWikiPage
{
#pragma warning disable CA2213
    [Export]
    private StageInfoBox infoBox = null!;
#pragma warning restore CA2213

    public override string ParentPageName => "WikiRoot";

    public override void _Ready()
    {
        base._Ready();

        infoBox.Page = PageContent;
    }
}
