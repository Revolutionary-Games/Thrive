local light = Entity("light")

-- Transform
light.sceneNode = OgreSceneNodeComponent()
light.sceneNode.workingCopy.scale = Vector3(0.01, 0.01, 0.01)
light.sceneNode.workingCopy.position.z = 2.0;
light.sceneNode:touch()
light:addComponent(light.sceneNode)


lightComponent = OgreLightComponent()
lightComponent.workingCopy:setRange(200)
lightComponent:touch()
light:addComponent(lightComponent)
lightEntity = OgreEntityComponent(OgreEntityComponent.PT_SPHERE)
light:addComponent(lightEntity)
onupdate = OnUpdateComponent()
light:addComponent(onupdate)
local time = 0
onupdate.callback = function(entityId, milliseconds)
    time = time + milliseconds / 1000
    light.sceneNode.workingCopy.position.x = 5 * math.sin(time)
    light.sceneNode.workingCopy.position.y = 5 * math.cos(time)
    light.sceneNode:touch()
end



