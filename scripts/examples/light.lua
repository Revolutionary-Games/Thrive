local entity = Entity("light")
local lightComponent = OgreLightComponent()
lightComponent.properties.type = OgreLightComponent.LT_SPOTLIGHT
lightComponent:setRange(200)
lightComponent.properties.diffuseColour = ColourValue(1.0, 1.0, 0.0, 1.0)
entity:addComponent(lightComponent)
