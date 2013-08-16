class 'MovementOrganelle' (Organelle)

function MovementOrganelle:__init(force, torque)
    Organelle.__init(self)
    self.force = force
    self.torque = torque
end

local function moveMicrobe(microbe, force, milliseconds)
    if microbe.movementDirection:isZeroLength() then
        return
    end 
    if force:isZeroLength() then
        return
    end
    local forceMagnitude = force:dotProduct(
        microbe.movementDirection
    )
    if forceMagnitude > 0 then
        local impulseMagnitude = milliseconds * forceMagnitude / 1000
        local impulse = impulseMagnitude * force:normalisedCopy()
        microbe.rigidBody:applyCentralImpulse(
            microbe.sceneNode.transform.orientation * impulse
        )
    end
end


local function turnMicrobe(microbe, torque)
    if torque == 0 then
        return
    end
    debug("Target point: " .. tostring(microbe.facingTargetPoint))
    local transform = microbe.sceneNode.transform
    local targetDirection = microbe.facingTargetPoint - transform.position
    local localTargetDirection = transform.orientation:Inverse() * targetDirection
    assert(localTargetDirection.z < 0.01, "Microbes should only move in the 2D plane with z = 0")
    local alpha = math.atan2(
        -localTargetDirection.x,
        localTargetDirection.y
    )
    if math.abs(math.deg(alpha)) > 1 then
        microbe.rigidBody:applyTorque(
            Vector3(0, 0, torque * alpha)
        )
    end
end


function MovementOrganelle:update(microbe, milliseconds)
    turnMicrobe(microbe, self.torque)
    moveMicrobe(microbe, self.force, milliseconds)
end

