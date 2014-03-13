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
    if direction:isZeroLength() then
        return
    end 
    if self.force:isZeroLength() then
        return
    end
    local forceMagnitude = self.force:dotProduct(direction)
    if forceMagnitude > 0 then
        local energy = math.abs(self.energyMultiplier * forceMagnitude * milliseconds / 1000)
        local availableEnergy = microbe:takeAgent(1, energy)
        if availableEnergy < energy then
            forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000 / milliseconds
        end
        local impulseMagnitude = milliseconds * forceMagnitude / 1000
        local impulse = impulseMagnitude * direction
        local a = microbe.sceneNode.transform.orientation * impulse
        microbe.rigidBody:applyCentralImpulse(
            microbe.sceneNode.transform.orientation * impulse
        )
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
            Vector3(0, 0, self.torque * alpha  )
        )
    end
end


function MovementOrganelle:update(microbe, milliseconds)
    Organelle.update(self, microbe, milliseconds)
    self:_turnMicrobe(microbe)
    self:_moveMicrobe(microbe, milliseconds)
end

