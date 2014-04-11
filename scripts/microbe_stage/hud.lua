-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
end


function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    local energy = playerMicrobe:getCompoundAmount(CompoundRegistry.getCompoundId("atp"))
    local energyTextOverlay = Entity("hud.energyCount"):getComponent(TextOverlayComponent.TYPE_ID)
    energyTextOverlay.properties.text = string.format("Energy: %d", energy+0.5)
    energyTextOverlay.properties:touch()

    local FONT_HEIGHT = 18 -- Not sure how to determine this correctly
    local compoundsString =  "Compounds: "
    local compoundCountsString =  ""
    local numberOfCompoundTypes = 0
    for compoundID in CompoundRegistry.getCompoundList() do
        numberOfCompoundTypes = numberOfCompoundTypes + 1
        compoundsString = compoundsString .. string.format("\n%-10s", CompoundRegistry.getCompoundDisplayName(compoundID))
        compoundCountsString = compoundCountsString .. string.format("\n -  %d", playerMicrobe:getCompoundAmount(compoundID)+0.5) -- round correctly 
    end
    local compoundsTextOverlay = Entity("hud.playerCompounds"):getComponent(TextOverlayComponent.TYPE_ID)
    compoundsTextOverlay.properties.text = compoundsString
    compoundsTextOverlay.properties.height = FONT_HEIGHT  + FONT_HEIGHT * numberOfCompoundTypes
    compoundsTextOverlay.properties.top = -2*FONT_HEIGHT -FONT_HEIGHT * numberOfCompoundTypes
    compoundsTextOverlay.properties:touch()
    local compoundCountsTextOverlay = Entity("hud.playerCompoundCounts"):getComponent(TextOverlayComponent.TYPE_ID)
    compoundCountsTextOverlay.properties.text = compoundCountsString
    compoundCountsTextOverlay.properties.height = FONT_HEIGHT  + FONT_HEIGHT * numberOfCompoundTypes
    compoundCountsTextOverlay.properties.top = -2*FONT_HEIGHT -FONT_HEIGHT * numberOfCompoundTypes
    compoundCountsTextOverlay.properties:touch()
end

