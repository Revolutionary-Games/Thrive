class 'MovementOrganelle' (Organelle)

function MovementOrganelle:__init(force, torque)
    Organelle.__init(self)
    self.energyMultiplier = 0.025
    self.force = force
    self.torque = torque
end

local function sign(x)
    if x < 0 then
        return -1
    elseif x > 0 then
        return 1
    else
        return 0
    end
end

function MovementOrganelle:_moveMicrobe(milliseconds)
    local direction = self.microbe.microbe.movementDirection
    if direction:isZeroLength() then
        return
    end 
    if self.force:isZeroLength() then
        return
    end
    local forceMagnitude = self.force:dotProduct(direction)
    local energy = math.abs(self.energyMultiplier * forceMagnitude * milliseconds / 1000)
    local availableEnergy = self.microbe:takeAgent(1, energy)
    if availableEnergy < energy then
        forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000 / milliseconds
    end
    if forceMagnitude > 0 then
        local impulseMagnitude = milliseconds * forceMagnitude / 1000
        local impulse = impulseMagnitude * direction
        self.microbe.rigidBody:applyCentralImpulse(
            self.microbe.sceneNode.transform.orientation * impulse
        )
    end
end


function MovementOrganelle:_turnMicrobe()
    if self.torque == 0 then
        return
    end
    local transform = self.microbe.sceneNode.transform
    local targetDirection = self.microbe.microbe.facingTargetPoint - transform.position
    local localTargetDirection = transform.orientation:Inverse() * targetDirection
    assert(localTargetDirection.z < 0.01, "Microbes should only move in the 2D plane with z = 0")
    local alpha = math.atan2(
        -localTargetDirection.x,
        localTargetDirection.y
    )
    if math.abs(math.deg(alpha)) > 1 then
        self.microbe.rigidBody:applyTorque(
            Vector3(0, 0, self.torque * alpha)
        )
    end
end


function MovementOrganelle:update(microbe, milliseconds)
    self:_turnMicrobe()
    self:_moveMicrobe(milliseconds)
end

