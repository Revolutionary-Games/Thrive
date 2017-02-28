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


Renames and different function signatures
-----------------------------------------

Renamed `CollisionShape.Axis.AXIS_X` to `SHAPE_AXIS.X`
also same with Y and Z


`CollisionFilter.collisions` now constructs a table from the iterator
that the actual `.collisions` method returns


`Keyboard.KeyCode` is now global enum `KEYCODE`


`Ogre::Ray::intersects` now returns a tuple


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











