class 'Organelle'

function Organelle:__init()
    self.entity = Entity()
    self.sceneNode = OgreSceneNodeComponent()
    self.entity:addComponent(self.sceneNode)
    self.collisionShape = btCompoundShape()
    self._hexes = {}
    self.position = {
        q = 0,
        r = 0
    }
end


function Organelle:addHex(q, r)
    for _, hex in ipairs(self._hexes) do
        if hex.q == q and hex.r == r then
            return false
        end
    end
    local hex = {
        q = q,
        r = r,
        entity = Entity(),
        collisionShape = btSphereShape(HEX_SIZE)
    }
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(x, y, 0)
    -- Scene node
    local hexSceneNode = OgreSceneNodeComponent()
    hexSceneNode.parent = self.entity
    hexSceneNode.transform.position = translation
    hexSceneNode.transform:touch()
    hex.entity:addComponent(hexSceneNode)
    hex.entity:addComponent(OgreEntityComponent("Hex.mesh"))
    -- Collision shape
    self.collisionShape:addChildShape(
        Quaternion(Radian(0), Vector3(1,0,0)),
        translation,
        hex.collisionShape
    )
    table.insert(self._hexes, hex)
    return true
end


function Organelle:removeHex(q, r)
    for i, hex in ipairs(self._hexes) do
        if hex.q == q and hex.r == r then
            table.remove(self._hexes, i)
            hex.entity:destroy()
            self.collisionShape:removeChildShape(hex.collisionShape)
            return true
        end
    end
    return false
end


function Organelle:update(microbe, milliseconds)
    -- Nothing
end

