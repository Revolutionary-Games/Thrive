Thrive Architecture
===================

This document outlines the main points of the Thrive architecture.

Thrive uses the Godot Engine and as such is structured out of Godot
Nodes that form scenes that are attached in the scene tree. There's a
separate document describing how to learn and work with the [Godot
Engine](learning_godot.md). That is very much recommended reading.

While Thrive mostly conforms to the usual way of using Godot (and
that's why it's important for Thrive contributors to be familiar with
Godot), we have added various extensions to in-built Godot systems or
added new utility systems that were missing from Godot. This makes it
so that not all standard Godot usage is good practice in Thrive
code. Some examples of this include our custom input system, extended
functionality GUI Nodes, and other smaller helper methods. These can
be learned by reading the documents in this folder (if available about
a specific system) or by reading other Thrive code and using the same
approach. That second way is also a good way to get familiar with the
[Thrive style](style_guide.md) and become familiar with various parts
of the codebase that provide commonly needed operations.

The remainder of this document describes the overall code architecture
of Thrive. For GUI Thrive uses Godot as intended, but for gameplay
code we use an Entity Component System (ECS) architecture where we
ourselves simulate the gameplay portion and only use Godot Nodes to
display things, play sounds, and of course show the GUI to the player.


Entity Component System (ECS)
-----------------------------

This document is not a tutorial about ECS, only very brief mentions of
the underlying concepts are used. If you are not familiar with ECS
here are a few learning resources:
- https://en.wikipedia.org/wiki/Entity_component_system
- https://github.com/SanderMertens/ecs-faq
- https://www.simplilearn.com/entity-component-system-introductory-guide-article
- https://medium.com/source-true/why-is-entity-component-system-ecs-so-awesome-for-game-development-f554e1367c17

### Components

Game entities in Thrive consist of various components that specify the
properties of that entity. For example to make a microbe it needs
various components setup with the right kind of data to function. To
help in spawning entities the class `SpawnHelpers` exists to create
various entity types.

In Thrive components are kept pure data containers by not having any
methods in the structs (other than constructors). Instead all
component "methods" are contained in an extension helper class which
is placed in the same file as the component. The helper class methods
should be provided and used for operations that aren't just as simple
as changing a single field in a component. This ensures that certain
operations are done in a consistent way and there aren't bugs caused
by an inconsistent way some action is applied to a component.

Some components contain one or more dirty flags. These tell the
systems that something has changed and they need to run. This is done
to improve performance by allowing skipping of processing components
that don't have anything interesting changes in them. When checking a
field in a component how it should be changed it is important to first
look for a helper method that sets it. If one doesn't exist then the
second thing to do is to check if there is a related dirty
flag. Documentation text on the field and the existing dirty flags
should make it clear if a related dirty flag exists. If you notice
that you changing a property doesn't do anything, the first thing to
check is to see if there is a related dirty flag.

Components are structs so that they are not heap allocated in C#,
instead being stack allocated. This increases the performance of the
system a ton, but adds some gotchas. The biggest being that components
should only be used through `ref` variables. Normal struct variables
in C# copy the entire struct memory each time they are assigned or
passed to a method. That's obviously bad if we need to copy each
component dozens of times per game update, so that's why `ref`
variables and parameters are very widely used when dealing with
components. Also extension methods dealing with components should use
`this ref` parameter type.

### Systems

Systems are the things that actually make the components do anything
useful. For example there is a system that applies an entity's
position to its visual Godot Node instance to make sure it shows up in
the right position on screen.

Systems should be split up along logical lines so that a single system
is not required to do too much. When systems have interdependencies
these are marked with attributes on the systems specifying the systems
they need to run after or before, and also which components the system
touches or just reads. The info on which components are used is very
important to be correct so that thread safety guarantees work (see the
next section on thread safety).

### Thread Safety

To improve the game performance a lot, multiple threads are used to
run entity systems in parallel. To do this safely systems must specify
which components they access or write to. This information is used to
create lists of systems that can run in parallel to each other. In
addition to systems running in parallel, the entities of a single
system can run in parallel as well. This is not done by default for
all systems as little work in multiple threads is slower than just a
single thread checking if there is anything to do.

Systems that run entities in parallel are passed in the real
background task executor and they have configured how many entities
are processed per thread. This number being too low will just reduce
game performance so it is important to tweak it correctly when adding
threading to a system.

Due to the multithreaded nature of the game simulation, it is not safe
to create, destroy or modify entity components during an update
run. Note that changing the data inside a component is safe and
perfectly normal. Care just needs to be taken to make sure the system
doing the changes has the right attributes marking which components it
writes to and if the system is threaded it needs to be made sure that
a single call to the update method doesn't modify multiple entities
that might be concurrently being modified by the same system. If those
are taken care of then the system can safely modify the data inside
components.

To facilitate changes to component structure (adding, removing)
`EntityCommandRecorder` type exists, which can be accessed from the
simulation world. With the recorder, changes can be queued to happen
after the current world simulation update run has finished. Similarly
the entity spawn methods actually just queue the spawning of the
entity. Once done with a recorder it needs to be returned to the
world, otherwise it is leaked. The normal entity destroy method
already uses a queue behind the scenes so it is safe to call at any
time.

### Saving and Loading

The ECS architecture simplifies saving and loading as it is just a
matter of writing the component data to disk and loading it back
later. The saving system automatically handles converting entity
references to point to newly created instances after loading a save.

One thing that needs to be considered is which properties of a
component are saved. Dirty flags that are used for purely runtime
state, for example which visual Godot scene is loaded for an entity,
need to not be saved so that the entity visuals can be recreated after
loading a save. Also directly setting a visual node or a physics shape
are non-optimal as that data cannot be saved. Some system needs to
recreate the data on load, or the data can be loaded through
`PredefinedVisuals` for example for a save-proof way to load graphics
for a Spatial instance.


Folder Structure
----------------

The game's code is organized into folders based on which stage is the
first one to require certain feature. This means that a feature that
is in multicellular but was already needed in microbe stage has its
code residing in the `src/microbe_stage` folder (or a
subfolder). Besides the stage specific code folders there's general
folders for utility and GUI code. There's also an `engine` folder for
kind of lower level features that Godot Engine doesn't provide but we
have coded ourselves to be useful in Thrive.


Native Code
-----------

The game simulation uses native C++ code to do some heavy tasks and
interact with C++ libraries. For example the Jolt physics engine is
used by our C++ code, which is then in turn used from C#. This native
module is kept small as it is harder to compile the module, meaning
changing the code in it is harder to do for the random Thrive developer.


### C# Interop (P/Invoke)

To interact with the native code module from C# interop is used. The C
wrapper around the C++ code is in the file `CInterop.h` and the C# of
the wrapper are contained in the various `NativeMethods` partial
classes (the external method definitions are split up in multiple
files to be next to the C# side class that needs them). When calling
the native side methods the goal is to avoid as many memory copies as
possible, that's why many of the external method signatures are pretty
mangled and not very clear from a C# viewpoint.

P/Invoke can be learned more about by reading the Microsoft
documentation on that C# feature:
https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke
