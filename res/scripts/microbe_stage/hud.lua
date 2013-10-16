-- Updates the hud with relevant information
class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
end


function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)

    local energy = playerMicrobe:getAgentAmount(1)
    local energyTextOverlay = Entity("hud.energyCount"):getComponent(TextOverlayComponent.TYPE_ID)
    energyTextOverlay.properties.text = string.format("Energy: %d", energy)
    energyTextOverlay.properties:touch()

    local FONT_HEIGHT = 18 -- Not sure how to determine this correctly
    local agentsString =  "Agents: "
    for agentID in pairs(playerMicrobe.microbe.vacuoles) do
        agentsString = agentsString .. string.format("\nID %d: %d", agentID, playerMicrobe:getAgentAmount(agentID))
    end
    local agentsTextOverlay = Entity("hud.playerAgents"):getComponent(TextOverlayComponent.TYPE_ID)
    agentsTextOverlay.properties.text = agentsString
    agentsTextOverlay.properties.height = FONT_HEIGHT  + FONT_HEIGHT * #playerMicrobe.microbe.vacuoles
    agentsTextOverlay.properties.top = -2*FONT_HEIGHT -FONT_HEIGHT * #playerMicrobe.microbe.vacuoles
    agentsTextOverlay.properties:touch()
end

