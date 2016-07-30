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
