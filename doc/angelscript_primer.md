AngelScript Primer
==================

The following is a very brief and incomplete overview of AngelScript's syntax and some
of its quirks. This is mostly meant for programmers already familiar with at 
least one other programming language, but unfamiliar with AngelScript.

AngelScript syntax is very similar to C++ so knowing that helps.

- [Official manual](http://www.angelcode.com/angelscript/sdk/docs/manual/index.html)
- [Leviathan AngelScript documentation](https://leviathanengine.com/doc/develop/Documentation/html/d0/db5/angelscript_main.html)

Note: AngelScript doesn't support trailing commas so don't add a comma
after the last item in a list.

Working With Scripts
--------------------

At application startup, Thrive parses the specific module files in the
build/bin/Script folder. To move your edited scripts there run `cmake
..` in `thrive/build` folder. The microbe stage module is specified in
`scripts/microbe_stage/microbe_stage.levgm`.

There will be at some point script reloading during runtime when they
change. But it isn't currently finished and doesn't work properly. To
apply changes in the scripts, you have to restart the application.

Within AngelScript, you have access to all classes exposed by Thrive.


Variables
---------

```cpp
    // Easy as pie
    float variable = 3.1415f;
    
    // Variables are initialized automatically
    int i;
    Event@ event;
    
    assert(event is null);
    assert(i == 0);
```

Dictionaries
------------

```cpp
    dictionary myTable = {
        {"key1", 1},
        {"key 2", GenericEvent("some event")}
    };

    // Iterate over all entries in a table
    auto keys = myTable.getKeys();
    for(uint i = 0; i < keys.length(); ++i){
        doSomething(myTable[keys[i]]);
    }
```


Handles and References
---------------------

TODO: write about this

```cpp
MicrobeComponent@ thisIsAHandle;

// Handles are compared with the 'is' operator (this is like pointer address comparison in C++)
if(thisIsAHandle is other){
}

// And they are assigned with @. If you get errors about missing assignment operator then try this!
@thisIsAHandle = other;

```
