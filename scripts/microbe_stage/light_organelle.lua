--------------------------------------------------------------------------------
-- A light source organelle class
--------------------------------------------------------------------------------
class 'LightOrganelle' (OrganelleComponent)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Constructor
--
-- @param arguments.colour
-- The colour of the light (a table with r, g, and b, and alpha values,
-- all between 0.0 and 1.0).
--
-- @param arguments.range
-- The range of the light.
function LightOrganelle:__init(arguments, data)
    --making sure this doesn't run when load() is called
    if arguments == nil and data == nil then
        return
    end

    self.range = arguments.range
    self.r = arguments.colour.r
    self.g = arguments.colour.g
    self.b = arguments.colour.b
    self.alpha = arguments.colour.alpha

    --Searching for the center of the organelle.
    self.qCenter = 0
    self.rCenter = 0
    local hexNum = 0
    local hexes = OrganelleFactory.checkSize(data)
    for _, hex in pairs(hexes) do
        self.qCenter = self.qCenter + hex.q
        self.rCenter = self.rCenter + hex.r
        hexNum = hexNum + 1
    end

    self.qCenter = self.qCenter / hexNum
    self.rCenter = self.rCenter / hexNum

    return self
end

function LightOrganelle:load(storage)
    self.range = storage:get("range", 200)
    self.r = storage:get("r", 1.0)
    self.g = storage:get("g", 1.0)
    self.b = storage:get("b", 1.0)
    self.qCenter = storage:get("qCenter", 0)
    self.rCenter = storage:get("rCenter", 0)
    self.alpha = storage:get("alpha", 1.0)
end

function LightOrganelle:storage(storage)
    local storage = StorageContainer()
    storage:set("range", self.range)
    storage:set("r", self.r)
    storage:set("g", self.g)
    storage:set("b", self.b)
    storage:set("qCenter", self.qCenter)
    storage:set("rCenter", self.rCenter)
    storage:set("alpha", self.alpha)
    return storage
end

-- Overridded from Organelle:onAddedToMicrobe
function LightOrganelle:onAddedToMicrobe(microbe, q, r, rotation)
    --Setting up the ligth component.
    local lightComponent = OgreLightComponent()
    lightComponent.properties.type = OgreLightComponent.LT_POINT
    lightComponent:setRange(self.range)
    lightComponent.properties.diffuseColour = ColourValue(self.r, self.g, self.b, self.alpha)

    --Setting up the scene node component.
    local sceneNode = OgreSceneNodeComponent()
    local x, y = axialToCartesian(q + self.qCenter, r + self.rCenter)
    local translation = Vector3(x, y, 0) --maybe it should be Vector3(x, y, 1)?
    sceneNode.parent = microbe.entity
    sceneNode.transform.position = translation
    sceneNode.transform:touch()

    --Adding the components to a new entity.
    self.lightEntity = Entity()
    self.lightEntity:addComponent(lightComponent)
    self.lightEntity:addComponent(sceneNode)
    microbe.entity:addChild(self.lightEntity)
end

-- Overridded from Organelle:onRemovedFromMicrobe
function LightOrganelle:onRemovedFromMicrobe(microbe, q, r)
    --Destroying the entity created in LightOrganelle:onAddedToMicrobe().
    self.lightEntity:destroy()
end
