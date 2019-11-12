Engine Overview
===============

This page gives an overview of the most important systems and components
and how they fit together. It is meant for prospective contributors trying to 
get a grasp on how the engine works.

This document assumes that you have an understanding of [entities, components and systems](entity_component.md)

Graphics
--------

NOTE: this section is outdated. See:
[#897](https://github.com/Revolutionary-Games/Thrive/issues/897)

Thrive uses the Ogre3D graphics engine for rendering. Rendering is done as one
of the last things in each frame. The most notable systems are listed here.

### Scene Nodes

Everything that has a position and orientation in the game world has an 
associated Ogre::SceneNode.

The two components Position and RenderNode work in conjunction to add, remove and update
the underlying Ogre::SceneNode for entities.

Many graphics-related components only work if their entity also has an 
RenderNode.

You can "attach" a scene node (i.e. make its transformation relative)
to a parent by calling Ogre::SceneNode::removeFromParent (on the
child) and Ogre::SceneNode::addChild (on the parent).

Scene nodes can optionally display a 3D mesh at their position by
creating a Model component.

Input
-----

Mouse and keyboard input are handled by SDL2 in Leviathan engine. See
the engine documentation for the input classes (note: currently this
is quite lacking, help writing this is welcome)

Physics
-------

For physics, we use Bullet library. Rigid bodies are created by adding
a Physics component to an entity. The physics system then updates the
entity's Position based on the physics simulation.

The authority on the position and orientation is the Physics component.

Scripting
---------

To avoid frequent recompilation for minor tweaks, Thrive uses AngelScript to
supplement the C++ code. To expose C++ functions and classes to scripts they need to be registered to the AngelScript engine.

Scripts have access to a global `GetEngine()` and
`GetThriveGame()`. See the binding functions for what you can call.

The primary mechanism for extending Thrive via scripts is to derive
from the ScriptComponent and ScriptSystem class. AngelScript-defined
systems can then be added with a call to
GameWorld::RegisterScriptSystem and
GameWorld::RegisterScriptComponentType. See the AngelScript primer for
how AngelScript systems can get relevant components.
