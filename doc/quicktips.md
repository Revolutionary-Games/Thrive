#Quick Tips for Navigating the Source

Most current work happens in Lua, with C++ development generally providing little more than interfaces to CEGUI, Ogre, and all the other parts of the engine.

Before you begin, Google "Ogre3D", "CEGUI", "luabind", and "Entity Component System" so you know what they are.

*Last Updated: Dec 27, 2014

#Lua
* Lua files are loaded into the engine in the order specified in the manifests.
  * So, for any two files, the second one implicitly includes the first.
* The main_menu, microbe_stage, and microbe_editor each have some structure in common:
  * The foo_hud.lua file defines the interactions with the GUI. 
    * Each defines a FooHudSystem, simple ECS Systems that have no components (yet, anyway).
    * The :init method runs on program startup, and in these Systems sets up the callbacks for each button, and maybe saves a bit of extra state.
    * The :update method runs every turn of the game loop
    * See src/gui/CEGUIWindow.h for more details on the CEGUI features we currently have a Lua interface for.
  * Each folder also has a setup.lua file, which, naturally, does necessary setup for the game mode. They define two important things:
    * The necessary setup functions, that are called on startup
    * a small function at the end which calls Engine:createGameState, defining:
      * The Systems which will run in this GameState (and their order)
      * The setup function, which for simplicity just calls the functions defined above
* The rest of the files in the subfolders contain more specific bits of functionality:
  * Organelles are defined in microbe_stage/, each file defining a class for a certain type of organelle, and factory functions for all the different organelles of that type.
    * Organelles are created by calling OrganelleFactory:makeOrganelle(data) -- the OrganelleFactory takes care of finding the right function
  * The code for Microbes is overall currently a bit messy, and will probably change soon.
* The other files not in a subfolder define more general utility things -- hex coordinate math, lua interpretation in the in-game console, etc. Read as you need.
