--
-- BacteriaComponent: bacterial analogue to MicrobeComponent
-- 

class 'BacteriaComponent' (Component)

function BacteriaComponent:__init(speciesName)
    Component.__init(self)
    self.speciesName = speciesName
end

function BacteriaComponent:load(storage)
    Component.load(self, storage)
    self.speciesName = storage:get("speciesName", "Default_Bacterium")
end

function BacteriaComponent:storage()
    storage = Component.storage(self)
    storage:set("speciesName", self.speciesName)
    return storage
end

REGISTER_COMPONENT('BacteriaComponent', BacteriaComponent)

-- 
-- Glue code for creating the default bacterium species:
-- 

function defaultBacteriumSpecies()
    local speciesEntity = Entity("Default_Bacterium")
    local processorComponent = ProcessorComponent()
    speciesEntity:addComponent(processorComponent)

    oxygen = CompoundRegistry.getCompoundId("oxygen")
    glucose = CompoundRegistry.getCompoundId("glucose")
    co2 = CompoundRegistry.getCompoundId("co2")

    -- purge thresholds are lower than low thresholds so the bacteria will release what they make
    processorComponent:setThreshold(oxygen, 15, 60, 10)
    processorComponent:setThreshold(glucose, 15, 60, 10)
    -- force the bacteria to reduce co2 levels, without purging
    processorComponent:setThreshold(co2, 0, 0, 600)

    photo = BioProcessRegistry.getId("Photosynthesis")
    processorComponent:setCapacity(photo, 1)
end

-- 
-- Bacterium: wrapper for bacterium entities
-- 

class 'Bacterium'

function Bacterium.createBacterium(speciesName, pos)
    local entity = Entity()
    local bacteriaComponent = BacteriaComponent(speciesName)

    local rigidBody = RigidBodyComponent()
    rigidBody.properties.friction = 0.2
    rigidBody.properties.linearDamping = 0.8

    rigidBody.properties.shape = SphereShape(HEX_SIZE)
    rigidBody:setDynamicProperties(
        pos,
        Quaternion(Radian(Degree(math.random()*360)), Vector3(0, 0, 1)),
        Vector3(0, 0, 0),
        Vector3(0, 0, 0)
    )
    rigidBody.properties:touch()
    -- Scene node
    local sceneNode = OgreSceneNodeComponent()
    sceneNode.meshName = "mitochondrion.mesh"
    sceneNode.visible = true
    sceneNode.transform.scale = Vector3(1, 1, 1)
    sceneNode.transform.position = pos
    sceneNode.transform:touch()
    
    compoundBag = CompoundBagComponent()

    compoundBag:giveCompound(CompoundRegistry.getCompoundId("co2"), 30)
    compoundBag:setProcessor(Entity(speciesName):getComponent(ProcessorComponent.TYPE_ID))

    local reactionHandler = CollisionComponent()
    reactionHandler:addCollisionGroup("bacteria")

    local components = {
        compoundBag,
        bacteriaComponent,
        rigidBody,
        sceneNode,
        reactionHandler,
    }

    for _, component in ipairs(components) do
        entity:addComponent(component)
    end
    print("about to init bacterium")
    return Bacterium(entity)
end

Bacterium.COMPONENTS = {
    compoundBag = CompoundBagComponent.TYPE_ID,
    bacterium = BacteriaComponent.TYPE_ID,
    rigidBody = RigidBodyComponent.TYPE_ID,
    sceneNode = OgreSceneNodeComponent.TYPE_ID,
    collisionHandler = CollisionComponent.TYPE_ID,
}

function Bacterium:__init(entity)
    print("quag")
    self.entity = entity
    print ("init bacterium")
    for key, typeId in pairs(Bacterium.COMPONENTS) do
        print(key)
        local component = entity:getComponent(typeId)
        assert(component ~= nil, "Can't create bacterium from this entity, it's missing " .. key)
        self[key] = entity:getComponent(typeId)
    end
end

function Bacterium:update(milliseconds)
    self:purgeCompounds()
end

function Bacterium:purgeCompounds()
    for compoundId in CompoundRegistry.getCompoundList() do
        local amount = self.compoundBag:excessAmount(compoundId) * 0.5
        if amount > 0 then amount = self:takeCompound(compoundId, amount) end
        if amount > 0 then self:ejectCompound(compoundId, amount) end
    end
end

function Bacterium:takeCompound(compoundId, maxAmount)
    return self.compoundBag:takeCompound(compoundId, maxAmount)
end

function Bacterium:ejectCompound(compoundId, amount)
    createCompoundCloud(CompoundRegistry.getCompoundInternalName(compoundId), self.sceneNode.transform.position.x, self.sceneNode.transform.position.y, amount*5000)
end

-- 
-- System for bacteria
-- 

class 'BacteriaSystem' (System)

function BacteriaSystem:__init()
    System.__init(self)
    self.entities = EntityFilter(
        {
            BacteriaComponent,
            OgreSceneNodeComponent,
            RigidBodyComponent,
            CollisionComponent
        },
        true
    )
    self.bacteria = {}
end

function BacteriaSystem:init(gameState)
    System.init(self, "BacteriaSystem", gameState)
    self.entities:init(gameState)
end

function BacteriaSystem:shutdown()
    self.entities:shutdown()
end

function BacteriaSystem:update(renderTime, logicTime)
    for entityId in self.entities:removedEntities() do
        self.bacteria[entityId] = nil
    end
    for entityId in self.entities:addedEntities() do
        local microbe = Bacterium(Entity(entityId))
        self.bacteria[entityId] = microbe
    end
    self.entities:clearChanges()
    for _, bacterium in pairs(self.bacteria) do
        bacterium:update(logicTime)
    end
end

