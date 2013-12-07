-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
end


function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    local energy = playerMicrobe:getCompoundAmount(1)
    local energyTextOverlay = Entity("hud.energyCount"):getComponent(TextOverlayComponent.TYPE_ID)
    energyTextOverlay.properties.text = string.format("Energy: %d", energy)
    energyTextOverlay.properties:touch()

    local FONT_HEIGHT = 18 -- Not sure how to determine this correctly
    local compoundsString =  "Compounds: "
    local compoundCountsString =  ""
    for compoundID in pairs(playerMicrobe.microbe.vacuoles) do
        --Following string.format doesn't quite allign text as desired for unknown reasons. (Could be a non-monospace font problem)
        compoundsString = compoundsString .. string.format("\n%-10s", CompoundRegistry.getCompoundDisplayName(compoundID))
        compoundCountsString = compoundCountsString .. string.format("\n -  %d", playerMicrobe:getCompoundAmount(compoundID)) 
    end
    local compoundsTextOverlay = Entity("hud.playerCompounds"):getComponent(TextOverlayComponent.TYPE_ID)
    compoundsTextOverlay.properties.text = compoundsString
    compoundsTextOverlay.properties.height = FONT_HEIGHT  + FONT_HEIGHT * #playerMicrobe.microbe.vacuoles
    compoundsTextOverlay.properties.top = -2*FONT_HEIGHT -FONT_HEIGHT * #playerMicrobe.microbe.vacuoles
    compoundsTextOverlay.properties:touch()
    local compoundCountsTextOverlay = Entity("hud.playerCompoundCounts"):getComponent(TextOverlayComponent.TYPE_ID)
    compoundCountsTextOverlay.properties.text = compoundCountsString
    compoundCountsTextOverlay.properties.height = FONT_HEIGHT  + FONT_HEIGHT * #playerMicrobe.microbe.vacuoles
    compoundCountsTextOverlay.properties.top = -2*FONT_HEIGHT -FONT_HEIGHT * #playerMicrobe.microbe.vacuoles
    compoundCountsTextOverlay.properties:touch()
end

