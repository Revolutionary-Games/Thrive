local entity = Entity("monster")
local sceneNodeComponent = OgreSceneNodeComponent()
sceneNodeComponent.meshName = "monster.mesh"
sceneNodeComponent.transform.position = Vector3(5, 0, 2)
entity:addComponent(sceneNodeComponent)
