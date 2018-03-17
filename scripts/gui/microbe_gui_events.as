CEGUI::Window@ atpBar;
CEGUI::Window@ atpCountLabel;
CEGUI::Window@ atpMaxLabel;
CEGUI::Window@ atpCountLabel2;

[@Listener="OnInit"]
void setupHUDBars(GuiObject@ instance){

    auto microbeRootWindow =
        instance.GetOwningManager().GetRootWindow().GetChild("MicrobeStageRoot");

    assert(microbeRootWindow !is null, "MicrobeStageRoot window is missing");

    auto compoundsScroll =
        microbeRootWindow.GetChild("CompoundPanel/CompoundScroll");
    
    assert(compoundsScroll !is null, "Compound panel not found (CompoundsScroll)");
    

    @atpBar = compoundsScroll.GetChild("ATPBar/ATPBar");

    assert(atpBar !is null, "GUI didn't find atpBar");

    @atpCountLabel = atpBar.GetChild("NumberLabel");
    @atpMaxLabel = compoundsScroll.GetChild("ATPBar/ATPTotal");
    @atpCountLabel2 = microbeRootWindow.GetChild("HealthPanel/ATPValue");

    assert(atpCountLabel !is null, "GUI didn't find atpCountLabel");
    assert(atpMaxLabel !is null, "GUI didn't find atpMaxLabel");
    assert(atpCountLabel2 !is null, "GUI didn't find atpCountLabel2");

    // TODO: shouldn't these background images be set in the .layout file?
    atpBar.SetProperty("FillImage", "ThriveGeneric/ATPBar");
}

[@Listener="Generic", @Type="PlayerCompoundAmounts"]
void handleCompoundBarsUpdate(GuiObject@ instance, GenericEvent@ event){

    NamedVars@ vars = event.GetNamedVars();
    auto atp = vars.GetSingleValueByName("compoundATP");

    if(atp !is null){

        auto atpAmount = double(atp);

        // atpBar.progressbarSetProgress(MicrobeSystem.getCompoundAmount(player,
        //         CompoundRegistry.getCompoundId("atp"))/(microbeComponent.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("atp"))));
        atpCountLabel.SetText(formatFloat(floor(atpAmount)));
        atpMaxLabel.SetText("/ ");
            // math.floor(microbeComponent.capacity/CompoundRegistry.getCompoundUnitVolume(CompoundRegistry.getCompoundId("atp"))));

        atpCountLabel2.SetText(formatFloat(floor(atpAmount)));
        
    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing atp value");
    }
}
