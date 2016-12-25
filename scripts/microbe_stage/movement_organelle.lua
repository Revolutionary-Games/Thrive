-- Enables a microbe to move and turn
class 'MovementOrganelle' (OrganelleComponent)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Calculate the momentum of the movement organelle based on angle towards nucleus
local function calculateForce(q, r, momentum)
print("calculateForce")
    local organelleX, organelleY = axialToCartesian(q, r)
    local nucleusX, nucleusY = axialToCartesian(0, 0)
    local deltaX = nucleusX - organelleX
    local deltaY = nucleusY - organelleY
    local dist = math.sqrt(deltaX^2 + deltaY^2) -- For normalizing vector
    local momentumX = deltaX / dist * momentum
    local momentumY = deltaY / dist * momentum
    local force = Vector3(momentumX, momentumY, 0.0)
    return force
    
end

-- Constructor
--
-- @param arguments.momentum
--  The force this organelle can exert to move a microbe.
--
-- @param arguments.torque
--  The torque this organelle can exert to turn a microbe.
function MovementOrganelle:__init(arguments, data)
    --making sure this doesn't run when load() is called
    if arguments == nil and data == nil then
        return
    end
    

    self.energyMultiplier = 0.025
    self.force = calculateForce(data.q, data.r, arguments.momentum)    
    print("init: " .. tostring(self.force))
    self.torque = arguments.torque
    self.backwards_multiplier = 0
	self.x = 0
	self.y = 0
	self.angle = 0
    self.movingTail = false
    return self
end

function MovementOrganelle:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    local organelleX, organelleY = axialToCartesian(q, r)
    local nucleusX, nucleusY = axialToCartesian(0, 0)
    local deltaX = nucleusX - organelleX
    local deltaY = nucleusY - organelleY
    local angle = math.atan2(deltaY, deltaX)
    if (angle < 0) then
        angle = angle + 2*math.pi
    end
    angle = (angle * 180/math.pi + 180) % 360
    
    self.rotation = angle
    organelle.sceneNode.transform.orientation = Quaternion(Radian(Degree(angle)), Vector3(0, 0, 1))
    organelle.sceneNode.transform:touch()
    
    organelle.sceneNode:playAnimation("Move", true)
    organelle.sceneNode:setAnimationSpeed(0.25)
end

function MovementOrganelle:load(storage)
    self.position = {}
    self.energyMultiplier = storage:get("energyMultiplier", 0.025)
    self.force = storage:get("force", Vector3(0,0,0))
    self.torque = storage:get("torque", 500)
end

function MovementOrganelle:storage()
    local storage = StorageContainer()
    storage:set("energyMultiplier", self.energyMultiplier)
    storage:set("force", self.force)
    storage:set("torque", self.torque)
    return storage
end

function MovementOrganelle:_moveMicrobe(microbe, organelle, milliseconds)
    local direction = microbe.microbe.movementDirection
    
    if microbe.microbe.isPlayerMicrobe then print(tostring(self.force.x)) end
    
    local forceMagnitude = self.force:dotProduct(direction)
    if forceMagnitude > 0 then
        if direction:isZeroLength() or self.force:isZeroLength()  then
            self.movingTail = false
            organelle.sceneNode:setAnimationSpeed(0.25)
            return
        end 
        self.movingTail = true
        organelle.sceneNode:setAnimationSpeed(1.3)
        
        local energy = math.abs(self.energyMultiplier * forceMagnitude * milliseconds / 1000)
        local availableEnergy = microbe:takeCompound(CompoundRegistry.getCompoundId("atp"), energy)
        if availableEnergy < energy then
            forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000 / milliseconds / self.energyMultiplier
            self.movingTail = false
            organelle.sceneNode:setAnimationSpeed(0.25)
        end
        local impulseMagnitude = microbe.microbe.movementFactor * milliseconds * forceMagnitude / 1000
        local impulse = impulseMagnitude * direction
        local a = microbe.sceneNode.transform.orientation * impulse
        microbe.rigidBody:applyCentralImpulse(
            microbe.sceneNode.transform.orientation * impulse
        )
    else 
        if self.movingTail then
            self.movingTail = false
            organelle.sceneNode:setAnimationSpeed(0.25)
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
            Vector3(0, 0, self.torque * alpha * microbe.microbe.movementFactor)
        )
        microbe.soundSource:playSound("microbe-movement-turn")
    end
end


function MovementOrganelle:update(microbe, organelle, logicTime)    
    local x, y = axialToCartesian(organelle.position.q, organelle.position.r)
    local membraneCoords = microbe.membraneComponent:getExternOrganellePos(x, y)
    local translation = Vector3(membraneCoords[1], membraneCoords[2], 0)
    organelle.sceneNode.transform.position = translation
    organelle.sceneNode.transform:touch()

    MovementOrganelle:_turnMicrobe(microbe)
    MovementOrganelle:_moveMicrobe(microbe, organelle, logicTime)
end
