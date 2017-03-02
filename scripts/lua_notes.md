How to debug easier
===================

Enable more checks
------------------

By default most Lua safety checks are off. To make debugging easier
you can build thrive with extra safety checks by specifying `LUA_CHECKS=ON`
when running cmake. Like this:

```bash
cmake .. -DLUA_CHECKS=ON
```


Changes with the switch to sol
==============================


Lua syntax changes
------------------

creating instances of class are like this:
`ColourValue.new(arguments)` instead of `ColourValue(arguments)`.


Function calls, MUST have `(` on the same line as the call. This is a syntax error:

```lua
self.id = currentSpawnSystem:addSpawnType
(
    ...
)
```

This is the correct syntax:

```lua
self.id = currentSpawnSystem:addSpawnType(
    ...
)
```


If you are getting errors about "attempt to call a string value"
without a stack trace. Then this is most likely what you have done
wrong.


### The .new method

Before all Lua objects were created like this:
`OgreSceneNodeComponent()` but now you need to call the `.new` method
like this: `OgreSceneNodeComponent.new()`. Though, for convenience
some classes expose (one of) their constructors the old way. For
example the vector class: `Vector3(1, 5, 0)` and `Degree` but only the
numeric constructors. So if you want to call `Degree` constructor with
a `Radian` you will need to use `Degree.new(radianVariable)`.

### C++ factories

Some types need to be created with factory functions. For example to
create a `SkyPlaneComponent` use `SkyPlaneComponent.factory()`. All
C++ Component types need to be created like this.


Renames and different function signatures
-----------------------------------------

Renamed `CollisionShape.Axis.AXIS_X` to `SHAPE_AXIS.X`
also same with Y and Z


`CollisionFilter.collisions` now constructs a table from the iterator
that the actual `.collisions` method returns


`Keyboard.KeyCode` is now global enum `KEYCODE`


`Ogre::Ray::intersects` now returns a tuple


### New calls to C++ functions taking GameState arguments

Now the GameState object is not exposed to C++ instead there is a
wrapper that needs to be passed to C++. It is stored in
`GameState.wrapper` so now `self.entities:init(gameState)` becomes
`self.entities:init(gameState.wrapper)`


If you don't pass the wrapper you will get a segmentation fault. But
if you have turned on LUA_CHECKS then you will get an error message
like this: `stack index 2, expected userdata, received table`



C++ notes
---------

C++ systems now receive a `GameStateData` object that allows them to
query stuff that Lua has setup.


Don't screw up by using `sol::var` when you should have used
`sol::property`, this is specially when binding lambdas or other
*property* accessors.


#### Warning

>Do NOT save the return type of a function_result with auto, as in
>`auto numwoof = woof(20);`, and do NOT store it anywhere.

From here: [safety - sol](https://sol2.readthedocs.io/en/latest/safety.html)











