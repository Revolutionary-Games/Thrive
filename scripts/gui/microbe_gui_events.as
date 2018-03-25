CEGUI::Window@ atpBar;
// Workaround to not being able to call the base class methods from derived
CEGUI::ProgressBar@ atpBarAsBar;
CEGUI::Window@ atpCountLabel;
CEGUI::Window@ atpMaxLabel;
CEGUI::Window@ atpCountLabel2;

CEGUI::Window@ oxygenBar;
CEGUI::ProgressBar@ oxygenBarAsBar;
CEGUI::Window@ oxygenCountLabel;
CEGUI::Window@ oxygenMaxLabel;

CEGUI::Window@ aminoacidsBar;
CEGUI::ProgressBar@ aminoacidsBarAsBar;
CEGUI::Window@ aminoacidsCountLabel;
CEGUI::Window@ aminoacidsMaxLabel;

CEGUI::Window@ ammoniaBar;
CEGUI::ProgressBar@ ammoniaBarAsBar;
CEGUI::Window@ ammoniaCountLabel;
CEGUI::Window@ ammoniaMaxLabel;

CEGUI::Window@ glucoseBar;
CEGUI::ProgressBar@ glucoseBarAsBar;
CEGUI::Window@ glucoseCountLabel;
CEGUI::Window@ glucoseMaxLabel;

CEGUI::Window@ co2Bar;
CEGUI::ProgressBar@ co2BarAsBar;
CEGUI::Window@ co2CountLabel;
CEGUI::Window@ co2MaxLabel;

CEGUI::Window@ fattyacidsBar;
CEGUI::ProgressBar@ fattyacidsBarAsBar;
CEGUI::Window@ fattyacidsCountLabel;
CEGUI::Window@ fattyacidsMaxLabel;

CEGUI::Window@ oxytoxyBar;
CEGUI::ProgressBar@ oxytoxyBarAsBar;
CEGUI::Window@ oxytoxyCountLabel;
CEGUI::Window@ oxytoxyMaxLabel;


CEGUI::Window@ hitpointsBar;
CEGUI::ProgressBar@ hitpointsBarAsBar;
CEGUI::Window@ hitpointsCountLabel;
CEGUI::Window@ hitpointsMaxLabel;


[@Listener="OnInit"]
void setupHUDBars(GuiObject@ instance){

    auto microbeRootWindow =
        instance.GetOwningManager().GetRootWindow().GetChild("MicrobeStageRoot");

    assert(microbeRootWindow !is null, "MicrobeStageRoot window is missing");

    auto compoundsScroll =
        microbeRootWindow.GetChild("CompoundPanel/CompoundScroll");
    
    assert(compoundsScroll !is null, "Compound panel not found (CompoundsScroll)");
    

    @atpBar = compoundsScroll.GetChild("ATPBar/ATPBar");
    @atpBarAsBar = cast<CEGUI::ProgressBar>(atpBar);
	
	@oxygenBar = compoundsScroll.GetChild("OxygenBar/OxygenBar");
    @oxygenBarAsBar = cast<CEGUI::ProgressBar>(oxygenBar);
	
	@aminoacidsBar = compoundsScroll.GetChild("AminoAcidsBar/AminoAcidsBar");
    @aminoacidsBarAsBar = cast<CEGUI::ProgressBar>(aminoacidsBar);
	
	@ammoniaBar = compoundsScroll.GetChild("AmmoniaBar/AmmoniaBar");
    @ammoniaBarAsBar = cast<CEGUI::ProgressBar>(ammoniaBar);
	
	@glucoseBar = compoundsScroll.GetChild("GlucoseBar/GlucoseBar");
    @glucoseBarAsBar = cast<CEGUI::ProgressBar>(glucoseBar);
	
	@co2Bar = compoundsScroll.GetChild("CO2Bar/CO2Bar");
    @co2BarAsBar = cast<CEGUI::ProgressBar>(co2Bar);
	
	@fattyacidsBar = compoundsScroll.GetChild("FattyAcidsBar/FattyAcidsBar");
    @fattyacidsBarAsBar = cast<CEGUI::ProgressBar>(fattyacidsBar);
	
	@oxytoxyBar = compoundsScroll.GetChild("OxyToxyNTBar/OxyToxyNTBar");
    @oxytoxyBarAsBar = cast<CEGUI::ProgressBar>(oxytoxyBar);

    @hitpointsBar = microbeRootWindow.GetChild("HealthPanel/LifeBar");
    @hitpointsBarAsBar = cast<CEGUI::ProgressBar>(hitpointsBar);

    assert(atpBar !is null && atpBarAsBar !is null, "GUI didn't find atpBar");
	assert(oxygenBar !is null && oxygenBarAsBar !is null, "GUI didn't find oxygenBar");
	assert(aminoacidsBar !is null && aminoacidsBarAsBar !is null,
        "GUI didn't find aminoacidsBar");
	assert(ammoniaBar !is null && ammoniaBarAsBar !is null, "GUI didn't find ammoniaBar");
	assert(glucoseBar !is null && glucoseBarAsBar !is null, "GUI didn't find glucoseBar");
	assert(co2Bar !is null && co2BarAsBar !is null, "GUI didn't find co2Bar");
	assert(fattyacidsBar !is null && fattyacidsBarAsBar !is null,
        "GUI didn't find fattyacidsBar");
	assert(oxytoxyBar !is null && oxytoxyBarAsBar !is null, "GUI didn't find oxytoxyBar");
    assert(hitpointsBar !is null && hitpointsBarAsBar !is null,
        "GUI didn't find hitpointsBar");
	
    @atpCountLabel = atpBar.GetChild("NumberLabel");
    @atpMaxLabel = compoundsScroll.GetChild("ATPBar/ATPTotal");
    @atpCountLabel2 = microbeRootWindow.GetChild("HealthPanel/ATPValue");

	@oxygenCountLabel = oxygenBar.GetChild("NumberLabel");
    @oxygenMaxLabel = compoundsScroll.GetChild("OxygenBar/OxygenTotal");
	
	@aminoacidsCountLabel = aminoacidsBar.GetChild("NumberLabel");
    @aminoacidsMaxLabel = compoundsScroll.GetChild("AminoAcidsBar/AminoAcidsTotal");
	
	@ammoniaCountLabel = ammoniaBar.GetChild("NumberLabel");
    @ammoniaMaxLabel = compoundsScroll.GetChild("AmmoniaBar/AmmoniaTotal");
	
	@glucoseCountLabel = glucoseBar.GetChild("NumberLabel");
    @glucoseMaxLabel = compoundsScroll.GetChild("GlucoseBar/GlucoseTotal");
	
	@co2CountLabel = co2Bar.GetChild("NumberLabel");
    @co2MaxLabel = compoundsScroll.GetChild("CO2Bar/CO2Total");
	
	@fattyacidsCountLabel = fattyacidsBar.GetChild("NumberLabel");
    @fattyacidsMaxLabel = compoundsScroll.GetChild("FattyAcidsBar/FattyAcidsTotal");
	
	@oxytoxyCountLabel = oxytoxyBar.GetChild("NumberLabel");
    @oxytoxyMaxLabel = compoundsScroll.GetChild("OxyToxyNTBar/OxyToxyNTTotal");

    @hitpointsCountLabel = hitpointsBar.GetChild("NumberLabel");
    @hitpointsMaxLabel = microbeRootWindow.GetChild("HealthPanel/HealthTotal");

	
    assert(atpCountLabel !is null, "GUI didn't find atpCountLabel");
    assert(atpMaxLabel !is null, "GUI didn't find atpMaxLabel");
    assert(atpCountLabel2 !is null, "GUI didn't find atpCountLabel2");
	assert(oxygenCountLabel !is null, "GUI didn't find oxygenCountLabel");
    assert(oxygenMaxLabel !is null, "GUI didn't find oxygenMaxLabel");
	assert(aminoacidsCountLabel !is null, "GUI didn't find aminoacidsCountLabel");
    assert(aminoacidsMaxLabel !is null, "GUI didn't find aminoacidsMaxLabel");
	assert(ammoniaCountLabel !is null, "GUI didn't find ammoniaCountLabel");
    assert(ammoniaMaxLabel !is null, "GUI didn't find ammoniaMaxLabel");
	assert(glucoseCountLabel !is null, "GUI didn't find glucoseCountLabel");
    assert(glucoseMaxLabel !is null, "GUI didn't find glucoseMaxLabel");
	assert(co2CountLabel !is null, "GUI didn't find co2CountLabel");
    assert(co2MaxLabel !is null, "GUI didn't find co2aMaxLabel");
	assert(fattyacidsCountLabel !is null, "GUI didn't find fattyacidsCountLabel");
    assert(fattyacidsMaxLabel !is null, "GUI didn't find fattyacidsMaxLabel");
	assert(oxytoxyCountLabel !is null, "GUI didn't find oxytoxyCountLabel");
    assert(oxytoxyMaxLabel !is null, "GUI didn't find oxytoxyMaxLabel");
    assert(hitpointsCountLabel !is null, "GUI didn't find hitpointsCountLabel");
    assert(hitpointsMaxLabel !is null, "GUI didn't find hitpointsMaxLabel");
	
    // TODO: shouldn't these background images be set in the .layout file?
    atpBar.SetProperty("FillImage", "ThriveGeneric/ATPBar");
	oxygenBar.SetProperty("FillImage", "ThriveGeneric/OxygenBar");
	aminoacidsBar.SetProperty("FillImage", "ThriveGeneric/AminoAcidsBar");
	ammoniaBar.SetProperty("FillImage", "ThriveGeneric/AmmoniaBar");
	glucoseBar.SetProperty("FillImage", "ThriveGeneric/GlucoseBar");
	co2Bar.SetProperty("FillImage", "ThriveGeneric/CO2Bar");
	fattyacidsBar.SetProperty("FillImage", "ThriveGeneric/FattyAcidsBar");
	oxytoxyBar.SetProperty("FillImage", "ThriveGeneric/OxyToxyBar");
    hitpointsBar.SetProperty("FillImage", "ThriveGeneric/HitpointsBar");
}

[@Listener="Generic", @Type="PlayerCompoundAmounts"]
void handleCompoundBarsUpdate(GuiObject@ instance, GenericEvent@ event){
    NamedVars@ vars = event.GetNamedVars();
    auto atp = vars.GetSingleValueByName("compoundATP");
    auto atpMax = vars.GetSingleValueByName("ATPMax");
	auto oxygen = vars.GetSingleValueByName("compoundOxygen");
    auto oxygenMax = vars.GetSingleValueByName("OxygenMax");
	auto aminoacids = vars.GetSingleValueByName("compoundAminoacids");
    auto aminoacidsMax = vars.GetSingleValueByName("AminoacidsMax");
	auto ammonia = vars.GetSingleValueByName("compoundAmmonia");
    auto ammoniaMax = vars.GetSingleValueByName("AmmoniaMax");
	auto glucose = vars.GetSingleValueByName("compoundGlucose");
    auto glucoseMax = vars.GetSingleValueByName("GlucoseMax");
	auto co2 = vars.GetSingleValueByName("compoundCo2");
    auto co2Max = vars.GetSingleValueByName("Co2Max");
	auto fattyacids = vars.GetSingleValueByName("compoundFattyacids");
    auto fattyacidsMax = vars.GetSingleValueByName("FattyacidsMax");
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
	
	if(oxygen !is null && oxygenMax !is null){
        auto oxygenAmount = double(oxygen);
        auto max = double(oxygenMax);
		
        oxygenBarAsBar.SetProgress(oxygenAmount / max);
        oxygenCountLabel.SetText(formatFloat(floor(oxygenAmount)));

        oxygenMaxLabel.SetText("/" + formatFloat(floor(oxygenMax)));
		        
    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing oxygen value");
    } 
	
	if(aminoacids !is null && aminoacidsMax !is null){
        auto aminoacidsAmount = double(aminoacids);
        auto max = double(aminoacidsMax);
		
        aminoacidsBarAsBar.SetProgress(aminoacidsAmount / max);
        aminoacidsCountLabel.SetText(formatFloat(floor(aminoacidsAmount)));
        aminoacidsMaxLabel.SetText("/" + formatFloat(floor(aminoacidsMax)));
		        
    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing aminoacids value");
    } 
	
	if(ammonia !is null && ammoniaMax !is null){
        auto ammoniaAmount = double(aminoacids);
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
	
	if(co2 !is null && co2Max !is null){
        auto co2Amount = double(co2);
        auto max = double(co2Max);
		
        co2BarAsBar.SetProgress(co2Amount / max);
        co2CountLabel.SetText(formatFloat(floor(co2Amount)));
        co2MaxLabel.SetText("/" + formatFloat(floor(co2Max)));
		        
    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing co2 value");
    } 
	
	if(fattyacids !is null && fattyacidsMax !is null){
        auto fattyacidsAmount = double(fattyacids);
        auto max = double(fattyacidsMax);
		
        fattyacidsBarAsBar.SetProgress(fattyacidsAmount / max);
        fattyacidsCountLabel.SetText(formatFloat(floor(fattyacidsAmount)));
        fattyacidsMaxLabel.SetText("/" + formatFloat(floor(fattyacidsMax)));
		        
    } else {
        LOG_WARNING("Microbe HUD compound amount update is missing fattyacids value");
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
}
