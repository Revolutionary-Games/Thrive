-- Enables a microbe to move and turn

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Calculate the momentum of the movement organelle based on angle towards nucleus
local function calculateForce(q, r, momentum)
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

MovementOrganelle = class(
    OrganelleComponent,
    -- Constructor
    --
    -- @param arguments.momentum
    --  The force this organelle can exert to move a microbe.
    --
    -- @param arguments.torque
    --  The torque this organelle can exert to turn a microbe.
    function(self, arguments, data)

        OrganelleComponent.create(self, arguments, data)

        --making sure this doesn't run when load() is called
        if arguments == nil and data == nil then
            return
        end
        

        self.energyMultiplier = 0.025
        self.force = calculateForce(data.q, data.r, arguments.momentum)
        self.torque = arguments.torque
        self.backwards_multiplier = 0
        self.x = 0
        self.y = 0
        self.angle = 0
        self.movingTail = false        
        
    end
)



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

    self.sceneNode = organelle.sceneNode
	self.sceneNode.transform.orientation = Quaternion.new(Radian.new(Degree(angle)), Vector3(0, 0, 1))
	self.sceneNode.transform.position = organelle.position.cartesian
    self.sceneNode.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    self.sceneNode.transform:touch()
    self.sceneNode.parent = microbe.entity
    -- self.sceneNode is a nullptr because it is already added to an entity
    --organelle.organelleEntity:addComponent(self.sceneNode)
    
    self.sceneNode:playAnimation("Move", true)
    self.sceneNode:setAnimationSpeed(0.25)
    
    --Adding a mesh to the organelle.
    local mesh = organelleTable[organelle.name].mesh
    if mesh ~= nil then
        self.sceneNode.meshName = mesh
    end
end

function MovementOrganelle:load(storage)
    self.position = {}
    self.energyMultiplier = storage:get("energyMultiplier", 0.025)
    self.force = storage:get("force", Vector3(0,0,0))
    self.torque = storage:get("torque", 500)
end

function MovementOrganelle:storage()
    local storage = StorageContainer.new()
    storage:set("energyMultiplier", self.energyMultiplier)
    storage:set("force", self.force)
    storage:set("torque", self.torque)
    return storage
end

function MovementOrganelle:_moveMicrobe(microbe, milliseconds)
    local direction = microbe.microbe.movementDirection
    
    local forceMagnitude = self.force:dotProduct(direction)
    if forceMagnitude > 0 then
        if direction:isZeroLength() or self.force:isZeroLength()  then
            self.movingTail = false
            self.sceneNode:setAnimationSpeed(0.25)
            return
        end 
        self.movingTail = true
        self.sceneNode:setAnimationSpeed(1.3)
        
        local energy = math.abs(self.energyMultiplier * forceMagnitude * milliseconds / 1000)
        local availableEnergy = MicrobeSystem.takeCompound(microbe.entity, CompoundRegistry.getCompoundId("atp"), energy)
        if availableEnergy < energy then
            forceMagnitude = sign(forceMagnitude) * availableEnergy * 1000 / milliseconds / self.energyMultiplier
            self.movingTail = false
            self.sceneNode:setAnimationSpeed(0.25)
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
            self.sceneNode:setAnimationSpeed(0.25)
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
	microbe.microbe.microbetargetdirection = math.abs(math.deg(alpha))
    if math.abs(math.deg(alpha)) > 1 then
        microbe.rigidBody:applyTorque(
            Vector3(0, 0, self.torque * alpha * microbe.microbe.movementFactor)
        )
    end
end


function MovementOrganelle:update(microbe, organelle, logicTime)    
    local x, y = axialToCartesian(organelle.position.q, organelle.position.r)
    local membraneCoords = microbe.membraneComponent:getExternOrganellePos(x, y)
    local translation = Vector3(membraneCoords[1], membraneCoords[2], 0)
    self.sceneNode.transform.position = translation
    self.sceneNode.transform:touch()

    self:_turnMicrobe(microbe)
    self:_moveMicrobe(microbe, logicTime)
end
