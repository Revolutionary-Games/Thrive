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


--! @brief Returns a component from an entity
--!
--! Two alternative signatures:
--! getComponent(string entityname, GameState gameState, Component componentClass)
--! getComponent(Entity entity, Component componentClass)
function getComponent(entityNameOrEntity, gameStateOrClass, componentClass)

    local componentObj

    -- Detect which signature
    local param1Type = type(entityNameOrEntity)
    if(param1Type == "string" or param1Type == "number") then

        -- First signature
        componentObj = Entity.new(entityNameOrEntity,
                                  gameStateOrClass.wrapper):getComponent(
            componentClass.TYPE_ID)
        
    else

        -- Second signature
        componentClass = gameStateOrClass
        
        componentObj = entityNameOrEntity:getComponent(componentClass.TYPE_ID)
    end

    if componentObj == nil then
        -- No such component
        return nil
    end

    if componentClass.castFrom then
        
        return componentClass.castFrom(componentObj)
        
    else
        -- Unwrap ComponentWrapper

        -- We do an unsafe cast for performance reasons
        return ComponentWrapper.castFromUnsafe(componentObj).luaObj
    end
end


--! Unwraps a ComponentWrapper from component and returns the lua
--! object
--! @return Unwrapped lua object. Or nil if wrapped wasn't a
--! valid wrapper
--! @note This is a safe but slow way to unwrap. See
--! getComponent for a faster way, but one that isn't safe it isn't
--! certain that wrapped is actually a ComponentWrapper
function unwrapWrappedComponent(wrapped)

    -- Cast to ComponentWrapper
    if wrapped.luaObj == nil then

        wrapped = ComponentWrapper.castFrom(wrapped)
    end

    return wrapped.luaObj
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

--[[
Decorate a function with once to ensure it only runs once. Ever.
]]
function once(fn)
    local able = true
    function foo(...)
        local ret = nil
        if able then
            ret = fn(...)
            able = false
        end
        if ret ~= nil then return ret end
    end
    return foo
end

--[[
We need an extension of once that provides each "once" with an accompanying "enable", that
simply allows the once to run again
]]

function onceWithReset(fn)
    local active = true
    function run(...)
        local ret = nil
        if active then
            ret = fn(...)
            active = false
        end
        if ret ~= nil then return ret end
    end
    function enable()
        active = true
    end
    return {run = run, enable = enable}
end

--[[
Decorates two functions at a time, forcing them to happen in alternating order
Example use-case: coordinating work done when moving between gamestates
]]
function forceAlternating(in_first, in_second)
    local firsts_turn = true
    function do_first(...)
        local ret = nil
        if firsts_turn then
            if in_first ~= nil then ret = in_first(...) end
            firsts_turn = false
        end
        return ret
    end
    function do_second(...)
        local ret = nil
        if firsts_turn == false then
            if in_second ~= nil then ret = in_second(...) end
            firsts_turn = true
        end
        return ret
    end
    return {first = do_first, second = do_second}
end

--[[
No idea if this works, but should make the decorated function run
only once per each instance of the object with the decorated method.
]]
function oncePer(fn)
    function foo(...)
        t = {...}
        -- we need to know how the object is injected into the method -- by closure? by argument?
        if t[1]["__oncePer__"] == nil then t[1]["__oncePer__"] = {} end
        -- indexing the table with the function itself could be a source of issues
        if t[1]["__oncePer__"][fn] == nil then
            ret = fn(...)
            t[1]["__oncePer__"][fn] = true
            return ret
        end
    end
    return foo
end

--[[
Memoizes the decorated function.
Could use some optimization, probably.
Should probably be done in C++, actually.

It's actually quite a bit too ugly in lua, requiring loads of work to detect identity between arg tables
...without which it's useless, of course.
]]
function memoize(fn)
    memo = {}
    function foo(...)
        if memo[{...}] == nil then
            memo[{...}] = fn(...)
        end
        return memo[{...}]
    end
    return foo
end

function print_r (t, indent) -- alt version, abuse to http://richard.warburton.it
  local indent=indent or ''
  for key,value in pairs(t) do
    io.write(indent,'[',tostring(key),']')
    if type(value)=="table" then io.write(':\n') print_r(value,indent..'\t')
    else io.write(' = ',tostring(value),'\n') end
  end
end

--! Prints a value along with it's type
function print_t(val, name)

    if name == nil or type(name) ~= "string" then

        name = debug.getinfo(2).short_src .. ":" .. debug.getinfo(2).currentline
        
    end

    print("debug print " .. name .. ":")

    local printVal = "<userdata>"
    if type(val) ~= "userdata" then

        printVal = tostring(val)

    end
    
    print("type: " .. type(val) .. " value: " .. printVal)

    if type(val) == "table" then
        print_r(val)
    end

    print("end print")
    
end

--! @brief Returns true if s is not an empty string
function isNotEmpty(s)
    return not (s == nil or s == '')
end


-- Call for quick debugging. Prints the line of the function calling this
function printLine()
    print(debug.getinfo(2).currentline)
end

