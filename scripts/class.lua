-- Helper file for creating Lua classes that inherit from C++ classes

function createSubclass(baseClass)

   assert(baseClass ~= nil, "tried to create subclass of nil")

   local newClass = {}

   local metaTable = { __index = newClass }

   -- Helper for creating instances
   function newClass:_createInstance(...)

      -- Create base object first
      local baseInstance = baseClass.new(unpack(arg))
      
      local newinst = {}
      setmetatable(newinst, metaTable)

      -- Attach to the base class
      setmetatable(newinst, { __index = baseInstance })

      newinst["_asBase"] = baseInstance
      
      return newinst
   end

   return newClass
end

