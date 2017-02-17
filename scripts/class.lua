-- Helper file for creating Lua classes that inherit from C++ classes

function createSubclass(baseClass)

   assert(baseClass ~= nil, "tried to create subclass of nil")

   local newClass = {}

   local metaTable = { __index = newClass }

   -- Helper for creating instances
   -- arg must be a table of values to pass to base class or nil
   function newClass._createInstance(arg)

      -- Create base object first
      if arg ~= nil then
         local baseInstance = baseClass.new(unpack(arg))
      else
         local baseInstance = baseClass.new()
      end
      
      local newinst = {}
      setmetatable(newinst, metaTable)

      -- Attach to the base class
      setmetatable(newinst, { __index = baseInstance })

      newinst["_asBase"] = baseInstance
      
      return newinst
   end

   return newClass
end


-- Function for creating systems
-- overrideMethods is a table of the functions that replace defaults in System
-- "update" must be defined or an exception is thrown
-- if overrideMethods contains __init it is called when creating new instances
function createLuaSystem(overrideMethods)

   assert(overrideMethods ~= nil, "lua systems require at least one override")
   
   print("create system")
   print_r(overrideMethods)
   
   local newClass = createSubclass(LuaSystem)

   print("class object")
   print_r(newClass)

   -- Helper for creating instances
   function newClass.new()

      assert(overrideMethods ~= nil)

      print("class new. overrides")
      print_r(overrideMethods)
      print("class type:")
      print_r(newClass)
      
      local instance = newClass._createInstance({overrideMethods})

      if overrideMethods["__init"] ~= nil then

         overrideMethods.__init(instance)
      end

      return instance
   end

   print("finished class object")
   print_r(newClass)
   return newClass
end



