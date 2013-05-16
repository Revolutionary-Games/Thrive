local light = Entity("light")

-- Transform
light.transform = TransformComponent()
light.transform.workingCopy.scale = Vector3(0.01, 0.01, 0.01)
light.transform.workingCopy.position.z = 2.0;
light.transform:touch()
light:addComponent(light.transform)


lightSceneNode = OgreSceneNodeComponent()
light:addComponent(lightSceneNode)
lightComponent = OgreLightComponent()
lightComponent.workingCopy:setRange(200)
lightComponent:touch()
light:addComponent(lightComponent)
lightEntity = OgreEntityComponent(OgreEntityComponent.PT_SPHERE)
light:addComponent(lightEntity)
onupdate = OnUpdateComponent()
light:addComponent(onupdate)
time = 0
onupdate.callback = function(entityId, milliseconds)
    time = time + milliseconds / 1000
    light.transform.workingCopy.position.x = 5 * math.sin(time)
    light.transform.workingCopy.position.y = 5 * math.cos(time)
    light.transform:touch()
end



