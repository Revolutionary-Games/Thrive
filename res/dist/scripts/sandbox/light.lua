local light = Entity("light")

-- Transform
light.sceneNode = OgreSceneNodeComponent()
light.sceneNode.properties.scale = Vector3(0.01, 0.01, 0.01)
light.sceneNode.properties.position.z = 2.0;
light.sceneNode.properties:touch()
light:addComponent(light.sceneNode)


lightComponent = OgreLightComponent()
lightComponent:setRange(200)
light:addComponent(lightComponent)
lightEntity = OgreEntityComponent(OgreEntityComponent.PT_SPHERE)
light:addComponent(lightEntity)
onupdate = OnUpdateComponent()
light:addComponent(onupdate)
local time = 0
onupdate.callback = function(entityId, milliseconds)
    time = time + milliseconds / 1000
    light.sceneNode.properties.position.x = 5 * math.sin(time)
    light.sceneNode.properties.position.y = 5 * math.cos(time)
    light.sceneNode.properties:touch()
end



