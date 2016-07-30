Thrive
======

This is the code repository for Thrive. For more information, visit
http://revolutionarygamesstudio.com/.

Overview
----------------

Repository structure:
- assets: An SVN repository that doesn't follow with the git repository. This contains all the assets such as models and other binaries. Get it with SVN Checkout http://assets.revolutionarygamesstudio.com
- cmake_modules: Scripts used by cmake when building the project binaries to find the dependencies. The dependencies will, for windows users, be installed with the mingw_setup directory.
- contrib: Git submodules and other external code bases we use for the project.
- cpack: Script used by 7zip.
- doc: Documentation files. Contains style guide, engine overview and other useful documentation. .
- mingw_setup: Dependency installation for windows and linux->windows cross-compiling (cross-compiling currently deprecated). You will need to run this to build the project files.
- ogre_cfg: Ogre configuration files for resources and plugins. This is installed with distributions.
- scripts: Lua scripts that contain part of the codebase. Lua scripts are used for easier development and code here can then later be transferred to the C++ base for performance. We use luabind for binding.
- src: The C++ code base containing the engine and other sections.

Remember to get the luabind and google test submodules with git submodule update --init --recursive.

Getting Involved
----------------
Depending on what you want to contribute, you need to take different steps
to get your development environment set up.

### Script Authors
If you only want to modify the Lua scripts, you can obtain a 
working copy of the game from official releases here: http://thrivegame.forum-free.ca/t1276-thrive-releases#29017
alternatively you can request a newer version from developers or compile the project yourself.

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

### C++ Programmers
To compile Thrive yourself, you will not only need to clone this git 
repository, but also the Subversion [asset repository][asset_repository],
username and password both "anonymous".  The best place to put the assets 
is in your code repository's `assets` subdirectory. If, for whatever 
reason, you want to check it out to another place, you will have to modify 
the `ASSET_DIRECTORY` variable in the CMake setup. Offloading art assets 
into Subversion was necessary due to size constraints for GitHub repositories.

Windows developers should follow the procedure outlined in the [mingw setup guide][mingwsetupguide].

For the time being, Linux developers will have to manually set up their build 
environment. The dependencies at the time of this writing are

* gcc 4.7.3
* boost 1.52
* Ogre 1.8
* OIS 1.3
* Bullet 2.81
* OpenAL Soft 1.15.1
* Vorbis 1.3.3
* OGG 1.3.1
* CEGUI 0.8.3
* TinyXML 2.6.2

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

### Modellers, texture and GUI artists, and Sound Engineers
To work on the art assets you will want a working copy of the game.
You can find official releases here: http://thrivegame.forum-free.ca/t1276-thrive-releases#29017
alternatively you can request a newer version from developers, compile the project yourself,
or you can download from our (currently not updating and temporary) build server at ftp://nimbal.net/jenkins.
 
After you have obtained a working version of the game, you can place any new assets in the corresponding subdirectories:
sound, models, materials and gui and the game will automatically detect up your new files, which you can then use in scripts.
An example of modifying a script to use your model would be to open scripts/microbe_stage/setup.lua with a text editor and 
find the setupEmitter function and edit the line that says:
    sceneNode.meshName = "molecule.mesh"
to 
    sceneNode.meshName = "myNewModel.mesh"
Similarly you can find sections of the scripts that use other assets and replace the assets they use. (often in setup scripts)
If you are truly uncomfortable with editing scripts you can simply try stealing the names of existing assets. For example 
going into the sound subdirectory and stealing the name "microbe-theme-1.ogg" by renaming your new sound-file to that and the 
game will then play that sound instead.

To contribute assets you can contact a developer and provide that person with your assets and the developer can add the assets 
the official repository. It will at a later time be possible to commit to the subversion (SVN) server yourself. In the meantime you can
learn about subversion from a lot of easy-to-find tutorials. A recommended tool for windows is: [tortoise svn][tortoiseSVN].

Extra note for modellers:
Ogre (the graphics system that Thrive uses) uses it's own file format for materials, models (meshes) and skeletons. In order for your 
model to be used in Thrive it will need to be converted. You can contact a developer to do this for you or you can do it yourself.
A good tutorial for converting blender files can be found [here][blender_ogre_tutorial] Note that you should make sure to use a version of
blender that has a corresponding version of blender2ogre to do the conversion.

[blender_ogre_tutorial]: http://www.ogre3d.org/forums/viewtopic.php?f=8&t=79616&p=504590 "Blender to ogre tutorial"
[asset_repository]: http://assets.revolutionarygamesstudio.com/ "Asset Repository"
[tortoiseSVN]: http://tortoisesvn.net/docs/release/TortoiseSVN_en/ "Tortoise SVN"
[mingw]: http://www.github.com/Revolutionary-Games/Thrive/blob/master/mingw_setup/readme.txt "MinGW setup guide"
[styleguide]: http://www.github.com/Revolutionary-Games/Thrive/blob/master/doc/style_guide.dox "Styleguide"
[mingwsetupguide]: http://www.github.com/Revolutionary-Games/Thrive/blob/master/mingw_setup/readme.txt "mingw setup guide" 
