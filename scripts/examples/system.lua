
-- Create class MySystem, derived from System
class 'MySystem' (System)

-- Define constructor
function MySystem:__init()
    -- Do not forget to call the constructor of the base class
    System.__init(self) 
    self.entities = EntityFilter(
        {
            -- We only want to know about entities that have both a 
            -- MyComponent and an OgreSceneNodeComponent
            MyComponent,
            OgreSceneNodeComponent
        },
        -- Optional. If true, we can ask the EntityFilter for added and 
        -- removed entities
        true 
    )
end

-- Called once before the first call to update()
function MySystem:init(engine)
    -- Enable the EntityFilter
    self.entities:init()
end

-- Called once after the last call to update()
function MySystem:shutdown()
    -- Disable the EntityFilter
    self.entities:shutdown()
end

-- Called once every frame
function MySystem:update(milliseconds)
    for entityId in self.entities:removedEntities() do
        -- It's generally best to process removed entities first
        print("Entity removed: " .. tostring(entityId))
    end
    for entityId in self.entities:addedEntities() do
        print("Entity added: " .. tostring(entityId))
    end
    -- Only necessary if the EntityFilter actually records the changes
    self.entities:clearChanges() 
    for entityId in self.entities.entities() do
        print("Updating entity: " .. tostring(entityId))
    end
end

