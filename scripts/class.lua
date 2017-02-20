-- Class creation functions

-- see: http://lua-users.org/wiki/SimpleLuaClasses
-- Modified to use .new for instantiating classes to be consistent with C++ classes
function class(base, init)
   local c = {}    -- a new class instance
   if not init and type(base) == 'function' then
      init = base
      base = nil
   elseif type(base) == 'table' then
      -- our new class is a shallow copy of the base class!
      for i,v in pairs(base) do
         c[i] = v
      end
      c._base = base
   end
   -- the class will be the metatable for all its objects,
   -- and they will look up their methods in it.
   c.__index = c

   -- expose a constructor
   local mt = {}
   c.new = function(...)
      local obj = {}
      setmetatable(obj, c)
      if init then
         -- The init method must explicitly call base init
         init(obj,...)
      else 
         -- make sure that any stuff from the base class is initialized!
         if base and base.init then
            base.init(obj, ...)
         end
      end
      return obj
   end
   c.init = init
   c.is_a = function(self, klass)
      local m = getmetatable(self)
      while m do 
         if m == klass then return true end
         m = m._base
      end
      return false
   end
   setmetatable(c, mt)
   return c
end





-- Helper file for creating Lua classes that inherit from C++ classes
-- Creates a subclass of a C++ class
function createSubclass(baseClass)

   error("todo: fix this")

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


