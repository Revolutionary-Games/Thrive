CEGUI::Window@ atpBar;
// Workaround to not being able to call the base class methods from derived
CEGUI::ProgressBar@ atpBarAsBar;
CEGUI::Window@ atpCountLabel;
CEGUI::Window@ atpMaxLabel;
CEGUI::Window@ atpCountLabel2;

CEGUI::Window@ ammoniaBar;
CEGUI::ProgressBar@ ammoniaBarAsBar;
CEGUI::Window@ ammoniaCountLabel;
CEGUI::Window@ ammoniaMaxLabel;

CEGUI::Window@ glucoseBar;
CEGUI::ProgressBar@ glucoseBarAsBar;
CEGUI::Window@ glucoseCountLabel;
CEGUI::Window@ glucoseMaxLabel;


CEGUI::Window@ oxytoxyBar;
CEGUI::ProgressBar@ oxytoxyBarAsBar;
CEGUI::Window@ oxytoxyCountLabel;
CEGUI::Window@ oxytoxyMaxLabel;


CEGUI::Window@ hitpointsBar;
CEGUI::ProgressBar@ hitpointsBarAsBar;
CEGUI::Window@ hitpointsCountLabel;
CEGUI::Window@ hitpointsMaxLabel;


[@Listener="OnInit"]
int setupHUDBars(GuiObject@ instance){

    auto microbeRootWindow =
        instance.GetOwningManager().GetRootWindow().GetChild("MicrobeStageRoot");

    assert(microbeRootWindow !is null, "MicrobeStageRoot window is missing");

    auto compoundsScroll =
        microbeRootWindow.GetChild("CompoundPanel/CompoundScroll");

    assert(compoundsScroll !is null, "Compound panel not found (CompoundsScroll)");


    @atpBar = compoundsScroll.GetChild("ATPBar/ATPBar");
    @atpBarAsBar = cast<CEGUI::ProgressBar>(atpBar);


    @ammoniaBar = compoundsScroll.GetChild("AmmoniaBar/AmmoniaBar");
    @ammoniaBarAsBar = cast<CEGUI::ProgressBar>(ammoniaBar);

    @glucoseBar = compoundsScroll.GetChild("GlucoseBar/GlucoseBar");
    @glucoseBarAsBar = cast<CEGUI::ProgressBar>(glucoseBar);

    @oxytoxyBar = compoundsScroll.GetChild("OxyToxyNTBar/OxyToxyNTBar");
    @oxytoxyBarAsBar = cast<CEGUI::ProgressBar>(oxytoxyBar);

    @hitpointsBar = microbeRootWindow.GetChild("HealthPanel/LifeBar");
    @hitpointsBarAsBar = cast<CEGUI::ProgressBar>(hitpointsBar);

    assert(atpBar !is null && atpBarAsBar !is null, "GUI didn't find atpBar");
    assert(ammoniaBar !is null && ammoniaBarAsBar !is null, "GUI didn't find ammoniaBar");
    assert(glucoseBar !is null && glucoseBarAsBar !is null, "GUI didn't find glucoseBar");
    assert(oxytoxyBar !is null && oxytoxyBarAsBar !is null, "GUI didn't find oxytoxyBar");
    assert(hitpointsBar !is null && hitpointsBarAsBar !is null,
        "GUI didn't find hitpointsBar");

    @atpCountLabel = atpBar.GetChild("NumberLabel");
    @atpMaxLabel = compoundsScroll.GetChild("ATPBar/ATPTotal");
    @atpCountLabel2 = microbeRootWindow.GetChild("HealthPanel/ATPValue");

    @ammoniaCountLabel = ammoniaBar.GetChild("NumberLabel");
    @ammoniaMaxLabel = compoundsScroll.GetChild("AmmoniaBar/AmmoniaTotal");

    @glucoseCountLabel = glucoseBar.GetChild("NumberLabel");
    @glucoseMaxLabel = compoundsScroll.GetChild("GlucoseBar/GlucoseTotal");

    @oxytoxyCountLabel = oxytoxyBar.GetChild("NumberLabel");
    @oxytoxyMaxLabel = compoundsScroll.GetChild("OxyToxyNTBar/OxyToxyNTTotal");

    @hitpointsCountLabel = hitpointsBar.GetChild("NumberLabel");
    @hitpointsMaxLabel = microbeRootWindow.GetChild("HealthPanel/HealthTotal");


    assert(atpCountLabel !is null, "GUI didn't find atpCountLabel");
    assert(atpMaxLabel !is null, "GUI didn't find atpMaxLabel");
    assert(atpCountLabel2 !is null, "GUI didn't find atpCountLabel2");
    assert(ammoniaCountLabel !is null, "GUI didn't find ammoniaCountLabel");
    assert(ammoniaMaxLabel !is null, "GUI didn't find ammoniaMaxLabel");
    assert(glucoseCountLabel !is null, "GUI didn't find glucoseCountLabel");
    assert(glucoseMaxLabel !is null, "GUI didn't find glucoseMaxLabel");
    assert(oxytoxyCountLabel !is null, "GUI didn't find oxytoxyCountLabel");
    assert(oxytoxyMaxLabel !is null, "GUI didn't find oxytoxyMaxLabel");
    assert(hitpointsCountLabel !is null, "GUI didn't find hitpointsCountLabel");
    assert(hitpointsMaxLabel !is null, "GUI didn't find hitpointsMaxLabel");

    // TODO: shouldn't these background images be set in the .layout file?
    atpBar.SetProperty("FillImage", "ThriveGeneric/ATPBar");
    ammoniaBar.SetProperty("FillImage", "ThriveGeneric/AmmoniaBar");
    glucoseBar.SetProperty("FillImage", "ThriveGeneric/GlucoseBar");
    oxytoxyBar.SetProperty("FillImage", "ThriveGeneric/OxyToxyBar");
    hitpointsBar.SetProperty("FillImage", "ThriveGeneric/HitpointsBar");

    return 1;
}

[@Listener="Generic", @Type="PlayerCompoundAmounts"]
int handleCompoundBarsUpdate(GuiObject@ instance, GenericEvent@ event){
    NamedVars@ vars = event.GetNamedVars();
    auto atp = vars.GetSingleValueByName("compoundATP");
    auto atpMax = vars.GetSingleValueByName("ATPMax");
    auto ammonia = vars.GetSingleValueByName("compoundAmmonia");
    auto ammoniaMax = vars.GetSingleValueByName("AmmoniaMax");
    auto glucose = vars.GetSingleValueByName("compoundGlucose");
    auto glucoseMax = vars.GetSingleValueByName("GlucoseMax");
    auto oxytoxy = vars.GetSingleValueByName("compoundOxytoxy");
    auto oxytoxyMax = vars.GetSingleValueByName("OxytoxyMax");
    auto hitpoints = vars.GetSingleValueByName("hitpoints");
    auto hitpointsMax = vars.GetSingleValueByName("hitpointsMax");

    // Debug print for all the variables
    // LOG_WRITE("Event data: \n" + vars.Serialize(" "));

    if(atp !is null && atpMax !is null){

        auto atpAmount = double(atp);
        auto max = double(atpMax);

        atpBarAsBar.SetProgress(atpAmount / max);
        atpCountLabel.SetText(formatFloat(floor(atpAmount)));
        atpMaxLabel.SetText("/" + formatFloat(floor(atpMax)));

        atpCountLabel2.SetText(formatFloat(floor(atpAmount)));

    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing atp value");
    }


    if(ammonia !is null && ammoniaMax !is null){
        auto ammoniaAmount = double(ammonia);
        auto max = double(ammoniaMax);

        ammoniaBarAsBar.SetProgress(ammoniaAmount / max);
        ammoniaCountLabel.SetText(formatFloat(floor(ammoniaAmount)));
        ammoniaMaxLabel.SetText("/" + formatFloat(floor(ammoniaMax)));

    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing ammonia value");
    }

    if(glucose !is null && glucoseMax !is null){
        auto glucoseAmount = double(glucose);
        auto max = double(glucoseMax);

        glucoseBarAsBar.SetProgress(glucoseAmount / max);
        glucoseCountLabel.SetText(formatFloat(floor(glucoseAmount)));
        glucoseMaxLabel.SetText("/" + formatFloat(floor(glucoseMax)));

    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing glucose value");
    }

    if(oxytoxy !is null && oxytoxyMax !is null){
        auto oxytoxyAmount = double(oxytoxy);
        auto max = double(oxytoxyMax);

        oxytoxyBarAsBar.SetProgress(oxytoxyAmount / max);
        oxytoxyCountLabel.SetText(formatFloat(floor(oxytoxyAmount)));
        oxytoxyMaxLabel.SetText("/" + formatFloat(floor(oxytoxyMax)));

    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing oxytoxy value");
    }

    if(hitpoints !is null && hitpointsMax !is null){
        auto hitpointsAmount = int(hitpoints);
        auto max = int(hitpointsMax);

        hitpointsBarAsBar.SetProgress(hitpointsAmount / max);
        hitpointsCountLabel.SetText(formatInt(hitpointsAmount));
        hitpointsMaxLabel.SetText("/" + formatInt(hitpointsMax));

    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing hitpoints value");
    }

    return 1;
}
