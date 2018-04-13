Quick Tips for Navigating the Source
====================================

Most current work happens in AngelScript, with C++ development generally providing little more than interfaces to CEGUI, Ogre, and all the other parts of the engine and some gameplay systems.

Before you begin, Google "Ogre3D", "CEGUI", and "Entity Component System" so you know what they are. And check out the [Leviathan manual](https://leviathanengine.com/doc/develop/Documentation/html/index.html) that has the documentation for the engine classes and some documentation on classes exposed to AngelScript. 

*Last Updated: March 18, 2018

#Script
* Organelles are defined in microbe_stage/, each file defining a class for a certain type of organelle, and factory functions for all the different organelles of that type.
  * Organelles are created by calling `PlacedOrganelle(getOrganelleDefinition("name"), q, r, rotation)`
