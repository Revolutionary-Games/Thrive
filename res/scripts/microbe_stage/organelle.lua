class 'Organelle'

function Organelle:__init()
    self.entity = Entity()
    self.sceneNode = OgreSceneNodeComponent()
    self.entity:addComponent(self.sceneNode)
    self.collisionShape = CompoundShape()
    self._hexes = {}
    self.position = {
        q = 0,
        r = 0
    }
    self.microbe = nil
    self._colour = ColourValue(1,1,1,1)
    self._internalEdgeColour = ColourValue(0.5, 0.5, 0.5, 1)
    self._externalEdgeColour = ColourValue(0, 0, 0, 1)
end


function Organelle:addHex(q, r)
    assert(not self.microbe, "Cannot change organelle shape while it is in a microbe")
    local s = encodeAxial(q, r)
    if self._hexes[s] then
        return false
    end
    local hex = {
        q = q,
        r = r,
        entity = Entity(),
        ogreEntity = Engine.sceneManager:createEntity("hex.mesh")
        collisionShape = SphereShape(HEX_SIZE),
    }
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(x, y, 0)
    -- Scene node
    local hexSceneNode = OgreSceneNodeComponent()
    hexSceneNode.parent = self.entity
    hexSceneNode.transform.position = translation
    hexSceneNode.transform:touch()
    hexSceneNode:attachObject(hex.ogreEntity)
    hex.entity:addComponent(hexSceneNode)
    -- Collision shape
    self.collisionShape:addChildShape(
        translation,
        Quaternion(Radian(0), Vector3(1,0,0)),
        hex.collisionShape
    )
    self._hexes[s] = hex
    return true
end


function Organelle:getHex(q, r)
    local s = encodeAxial(q, r)
    return self._hexes[s]
end


function Organelle:onAddedToMicrobe(microbe, q, r)
    self.microbe = microbe
    self.position.q = q
    self.position.r = r
end


function Organelle:onRemovedFromMicrobe(microbe)
    assert(microbe == self.microbe, "Can't remove organelle, wrong microbe")
    self.microbe = nil
    self.position.q = 0
    self.position.r = 0
end


function Organelle:removeHex(q, r)
    assert(not self.microbe, "Cannot change organelle shape while it is in a microbe")
    local s = encodeAxial(q, r)
    local hex = table.remove(self._hexes, s)
    if hex then
        hex.entity:destroy()
        self.collisionShape:removeChildShape(hex.collisionShape)
        return true
    else
        return false
    end
end


function Organelle:setColour(colour)
    self._colour = colour
    self:updateHexColours()
end


function Organelle:update(microbe, milliseconds)
    -- Nothing
end


function Organelle:updateHexColours()
    for _, hex in pairs(self._hexes) do
        local center = hex.ogreEntity:getSubEntity("center")
        center:setMaterial(getColourMaterial(self._colour))
        for i, qs, rs in iterateNeighbours(hex.q, hex.r) do
            local neighbourHex = self:getHex(qs, rs)
            local neighbourOrganelle = self.microbe and self.microbe:getOrganelleAt(
                self.position.q + qs,
                self.position.r + rs
            )
            local sideName = HEX_SIDE_NAME[i]
            local subEntity = hex.ogreEntity:getSubEntity(sideName)
            local edgeColour = nil
            if neighbourHex then
                edgeColour = self._colour
            elseif neighbourOrganelle then
                edgeColour = self._internalEdgeColour
            else
                edgeColour = self._externalEdgeColour
            end
            subEntity:setMaterial(getColourMaterial(edgeColour))
        end
    end
end
