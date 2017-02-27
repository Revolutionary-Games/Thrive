
-- Create class MyComponent
MyComponent = class(

    -- Define constructor
    function(self)

        self.stuff = 20
        self.prey = nil
        
    end
)

-- Name the component
MyComponent.TYPE_NAME = "MyComponent"

-- To enable proper serialization, you must override both the storage()
-- and the load() (see below) functions
function MyComponent:storage(storage)
    
    storage:set("movementRadius", self.movementRadius)
    
end

function MyComponent:load(storage)
    Component.load(self, storage)
    self.movementRadius = storage:get("movementRadius", 20)

end

-- Register the new component type with the component factory 
REGISTER_COMPONENT("MyComponent", MyComponent)

