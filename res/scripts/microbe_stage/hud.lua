
--------------------------------------------------------------------------------
-- Hud system
--
-- Updates HUD
--------------------------------------------------------------------------------

class 'HudSystem' (System)

function HudSystem:__init()
    System.__init(self)
end


function HudSystem:update(milliseconds)
    local player = Entity("player")
    local playerMicrobe = Microbe(player)
    local energy = playerMicrobe:getAgentAmount(1)
    local textOverlay = Entity("hud.energyCount"):getComponent(TextOverlayComponent.TYPE_ID)
    textOverlay.properties.text = string.format("Energy: %d", energy)
    textOverlay.properties:touch()
end

