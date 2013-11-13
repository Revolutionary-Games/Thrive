-- Defines some utility functions for convenience

-- Registers a component type
--
-- Registering a component type enables serialization / deserialization for
-- that type.
--
-- @param name
--  A unique string that identifies this component type
--
-- @param cls
--  The class object of the component type
function REGISTER_COMPONENT(name, cls)
    Engine.componentFactory:registerComponentType(
        name,
        cls
    )
end


-- Gets a component from an entity, creating the component if it's not present
--
-- @param componentCls
--  The class object of the component type
function Entity:getOrCreate(componentCls)
    component = self:getComponent(componentCls.TYPE_ID)
    if component == nil then
        component = componentCls()
        self:addComponent(component)
    end
    return component
end


-- Computes a number's sign
--
-- @param x
--  A number
--
-- @returns sign
--  -1 if x is negative
--   0 if x is zero
--  +1 if x is positive
function sign(x)
    if x < 0 then
        return -1
    elseif x > 0 then
        return 1
    else
        return 0
    end
end

