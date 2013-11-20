
-- Create class MyComponent, derived from Component
class 'MyComponent' (Component)

-- Define constructor
function MyComponent:__init()
    -- Do not forget to call the constructor of the base class
    Component.__init(self) 
    self.data = 0
end

-- To enable proper serialization, you must override both the storage()
-- and the load() (see below) functions
function MyComponent:storage()
    local storage = Component.storage(self)
    storage:set("data", self.data)
end

function MyComponent:load(storage)
    Component.load(self, storage)
    self.data = storage:get("data", 0)
end

-- Register the new component type with the component factory 
REGISTER_COMPONENT("MyComponent", MyComponent)
