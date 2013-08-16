class 'Microbe'

function Microbe:__init(name)
    self.movementDirection = Vector3(0, 0, 0)
    self._organelles = {}
    self.facingTargetPoint = Vector3(0, 0, 0)
    if name then
        self.entity = Entity(name)
    else
        self.entity = Entity()
    end
    -- Rigid body
    self.rigidBody = RigidBodyComponent()
    self.rigidBody.properties.shape = btCompoundShape()
    self.rigidBody.properties.linearDamping = 0.5
    self.rigidBody.properties.friction = 0.2
    self.rigidBody.properties.linearFactor = Vector3(1, 1, 0)
    self.rigidBody.properties.angularFactor = Vector3(0, 0, 1)
    self.rigidBody.properties:touch()
    self.entity:addComponent(self.rigidBody)
    -- Scene node
    self.sceneNode = OgreSceneNodeComponent()
    self.entity:addComponent(self.sceneNode)
    -- OnUpdate
    self.onUpdate = OnUpdateComponent()
    self.onUpdate.callback = function(entityId, milliseconds)
        self:update(milliseconds)
    end
    self.entity:addComponent(self.onUpdate)
end


function Microbe:addOrganelle(q, r, organelle)
    table.insert(self._organelles, organelle)
    organelle.position.q = q
    organelle.position.r = r
    do
        -- Collision shape
        local x, y = axialToCartesian(organelle.position.q, organelle.position.r)
        local translation = Vector3(x, y, 0)
        self.rigidBody.properties.shape:addChildShape(
            Quaternion(Radian(0), Vector3(1,0,0)),
            translation,
            organelle.collisionShape
        )
    end
    organelle.sceneNode.parent = self.entity
    local x, y = axialToCartesian(q, r)
    organelle.sceneNode.transform.position = Vector3(x, y, 0)
    organelle.sceneNode.transform:touch()
end


function Microbe:update(milliseconds)
    for _, organelle in ipairs(self._organelles) do
        organelle:update(self, milliseconds)
    end
end


