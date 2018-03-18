Entities, Components and Systems
================================

Introductions to the entity / component approach can be found here:
 - [Entity / Component Game Design: A Primer](http://piemaster.net/2011/07/entity-component-primer/)
 - [Game Engines 101: The Entity / Component Model](http://www.gamasutra.com/blogs/MeganFox/20101208/88590/Game_Engines_101_The_EntityComponent_Model.php)
 - [What is an entity system framework?](http://www.richardlord.net/blog/what-is-an-entity-framework)

The following gives an overview of the implementation of entities, 
components and systems in Thrive.

GameWorld
---------

Entities and their components are managed by a GameWorld (microbe
stage uses CellStageWorld). The GameWorld identifies each entity by
its unique id. You can use GameWorld::CreateEntity() to obtain an
id. Don't forget to call GameWorld::QueueDestroyEntity when the entity
should be destroyed. This delayed destruction is used to aboid
problems with destroying components that are currently used during a
run.

An entity can have at most one component of each type. Component types
are distinguished by their C++ class or name if they are defined in
ANgelScript. To identify a component type C++ classes have a `TYPE`
member.

GameWorld represent a distinct state of the game with its own systems and 
entities. Examples for such a are "microbe stage" and "microbe editor".

All systems are managed by a GameWorld. The GameWorld provides entity 
management, initializes its systems, updates them during the game and 
finally, shuts them down.

GameWorlds are created in ThriveGame during setup.

Systems
-------

The absolute minimum a system has to implement is the Run(Leviathan::GameWorld& world) method
function. You can also add a constructor and a destructor for
setup and teardown procedures.

Usually, a system operates on entities that have a specific
combination of components. Inheriting from Leviathan::System and
implementing CreateNodes and DestroyNodes allows systems to keep track
of entities that have their required components. AngelScript systems
can use CreateAndDestroyNodes and use the helper function
ScriptSystemNodeHelper.

