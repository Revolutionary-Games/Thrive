local WIDTH = 200
local HEIGHT = 32

local energyCount = Entity("hud.energyCount")
energyCount.text = TextOverlayComponent("hud.energyCount")
energyCount:addComponent(energyCount.text)
energyCount.text.properties.horizontalAlignment = TextOverlayComponent.Center
energyCount.text.properties.verticalAlignment = TextOverlayComponent.Bottom
energyCount.text.properties.width = WIDTH
energyCount.text.properties.height = HEIGHT
energyCount.text.properties.left = -WIDTH / 2
energyCount.text.properties.top = -HEIGHT
energyCount.text.properties:touch()

function setPlayerEnergyCount(count)
    energyCount.text.properties.text = string.format("Energy: %d", count)
    energyCount.text.properties:touch()
end
