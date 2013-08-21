local light = Entity("light")

-- Transform
light.sceneNode = OgreSceneNodeComponent()
light.sceneNode.transform.scale = Vector3(0.01, 0.01, 0.01)
light.sceneNode.transform.position.z = 2.0;
light.sceneNode.transform:touch()
light.sceneNode:attachObject(
    Engine.sceneManager:createEntity(SceneManager.PT_SPHERE)
)
light:addComponent(light.sceneNode)


lightComponent = OgreLightComponent()
lightComponent:setRange(200)
light:addComponent(lightComponent)

onupdate = OnUpdateComponent()
light:addComponent(onupdate)
local time = 0
onupdate.callback = function(entityId, milliseconds)
    time = time + milliseconds / 1000
    light.sceneNode.transform.position.x = 5 * math.sin(time)
    light.sceneNode.transform.position.y = 5 * math.cos(time)
    light.sceneNode.transform:touch()
end



