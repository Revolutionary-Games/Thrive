-- Enables a microbe to move and turn
class 'MovementOrganelle' (Organelle)

-- Constructor
--
-- @param force
--  The force this organelle can exert to move a microbe
--
-- @param torque
--  The torque this organelle can exert to turn a microbe
function MovementOrganelle:__init(force, torque)
    Organelle.__init(self)
    self.energyMultiplier = 0.025
    self.force = force
    self.torque = torque
    self.backwards_multiplier = 0
end

function MovementOrganelle:onAddedToMicrobe(microbe, q, r)  
    Organelle.onAddedToMicrobe(self, microbe, q,r)
    self.tailEntity = Entity()
    local x, y = axialToCartesian(q, r)
    local translation = Vector3(0, 0, 0.5)
    sceneNode = OgreSceneNodeComponent()
    sceneNode.parent = self.entity
    sceneNode.transform.position = translation
    sceneNode.meshName = "Flagella.mesh"
    sceneNode:playAnimation("Move", true)
    sceneNode:setAnimationSpeed(0.25)
    sceneNode.transform.scale = Vector3(0.35, 0.6, 0.6)
    sceneNode.transform.orientation = Quaternion(Radian(Degree(0)), Vector3(0, 0, 1))
    sceneNode.transform:touch()
    self.tailEntity:addComponent(sceneNode)
    self.tailEntity.sceneNode = sceneNode
    self.tailEntity:setVolatile(true)
    self.movingTail = false
    local organelleX, organelleY = axialToCartesian(q, r)
    local nucleusX, nucleusY = axialToCartesian(0, 0)
    local deltaX = nucleusX - organelleX
    local deltaY = nucleusY - organelleY
    local angle = math.atan2(deltaY, deltaX)
    if (angle < 0) then
        angle = angle + 2*math.pi
    end
    angle = (angle * 180/math.pi + 180) % 360
    self.tailEntity:getComponent(OgreSceneNodeComponent.TYPE_ID).transform.orientation = Quaternion(Radian(Degree(angle)), Vector3(0, 0, 1))
end

function MovementOrganelle:onRemovedFromMicrobe(microbe)
    self.tailEntity:destroy() --ogre scenenode will be destroyed due to parenting but the rest of the entity wont without this call.
end

function MovementOrganelle:destroy()
    self.tailEntity:destroy()
    Organelle.destroy(self)
end

function MovementOrganelle:load(storage)
    Organelle.load(self, storage)
    self.energyMultiplier = storage:get("energyMultiplier", 0.025)
    self.force = storage:get("force", Vector3(0,0,0))
    self.torque = storage:get("torque", Vector3(0,0,0))
end

function MovementOrganelle:storage()
    local storage = Organelle.storage(self)
    storage:set("energyMultiplier", self.energyMultiplier)
    storage:set("force", self.force)
    storage:set("torque", self.torque)
    return storage
end

function MovementOrganelle:_moveMicrobe(microbe, milliseconds)
    local direction = microbe.microbe.movementDirection
    if direction:isZeroLength() or self.force:isZeroLength() then
        if self.movingTail then
            self.movingTail = false
            self.tailEntity.sceneNode:setAnimationSpeed(0.25)
        end
        return
    end 
    local forceMagnitude = self.force:dotProduct(direction)
    if forceMagnitude > 0 then
        if not self.movingTail then
            self.movingTail = true
            self.tailEntity.sceneNode:setAnimationSpeed(1.3)
        end
        local energy = math.abs(self.energyMultiplier * forceMagnitude * milliseconds / 1000)
        local availableEnergy = microbe:takeCompound(CompoundRegistry.getCompoundId("atp"), energy)
        if availableEnergy < energy then
            forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000 / milliseconds
        end
        local impulseMagnitude = milliseconds * forceMagnitude / 1000
        local impulse = impulseMagnitude * direction
        local a = microbe.sceneNode.transform.orientation * impulse
        microbe.rigidBody:applyCentralImpulse(
            microbe.sceneNode.transform.orientation * impulse
        )

    else 
        if self.movingTail then
        self.movingTail = false
         self.tailEntity.sceneNode:setAnimationSpeed(0.25)
        end
    end
end


function MovementOrganelle:_turnMicrobe(microbe)
    if self.torque == 0 then
        return
    end
    local transform = microbe.sceneNode.transform
    local targetDirection = microbe.microbe.facingTargetPoint - transform.position
    local localTargetDirection = transform.orientation:Inverse() * targetDirection
    localTargetDirection.z = 0 -- improper fix. facingTargetPoint somehow gets a non-zero z value.
    assert(localTargetDirection.z < 0.01, "Microbes should only move in the 2D plane with z = 0")
    local alpha = math.atan2(
        -localTargetDirection.x,
        localTargetDirection.y
    )
    if math.abs(math.deg(alpha)) > 1 then
        microbe.rigidBody:applyTorque(
            Vector3(0, 0, self.torque * alpha)
        )
        microbe.soundSource:playSound("microbe-movement-turn")
    end
end


function MovementOrganelle:update(microbe, logicTime)
    Organelle.update(self, microbe, logicTime)
    self:_turnMicrobe(microbe)
    self:_moveMicrobe(microbe, logicTime)
end

Organelle.mpCosts["flagellum"] = 25

-- factory functions
function OrganelleFactory.make_flagellum(data)
    -- Calculate the momentum of the movement organelle based on angle towards nucleus
    local organelleX, organelleY = axialToCartesian(data.q, data.r)
    local nucleusX, nucleusY = axialToCartesian(0, 0)
    local deltaX = nucleusX - organelleX
    local deltaY = nucleusY - organelleY
    local dist = math.sqrt(deltaX^2 + deltaY^2) -- For normalizing vector
    local momentumX = deltaX / dist * FLAGELIUM_MOMENTUM
    local momentumY = deltaY / dist * FLAGELIUM_MOMENTUM
    local flagellum = MovementOrganelle(
        Vector3(momentumX, momentumY, 0.0),
        300
    )
    flagellum:setColour(ColourValue(0.8, 0.3, 0.3, 1))
    flagellum:addHex(0, 0)
    return flagellum
end
