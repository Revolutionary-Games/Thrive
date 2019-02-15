Thrive
======

This is the code repository for Thrive. For more information, visit
[Revolutionary Games' Website](http://revolutionarygamesstudio.com/).

Overview
----------------
Repository structure:
- assets: An SVN repository that doesn't follow with the git repository. This contains all the assets such as models and other binaries. It is located at [asset repository][asset_repository] but it will be automatically downloaded by the setup script.
- doc: Documentation files. Contains style guide, engine overview and other useful documentation.
- scripts: AngelScript scripts that contain part of the codebase. Scripts are used for easier development and code here can then later be transferred to the C++ base for performance. 
- src: The C++ code base containing the common helper classes and gameplay code moved to C++.
- test: Contains tests that will ensure that core parts work correctly. These are currently really lacking.

Getting Involved
----------------
Depending on what you want to contribute, you need to take different steps
to get your development environment set up.

There are also other useful documents in the doc folder not mentioned here.

### Script Authors
If you only want to modify the AngelScript scripts, you can obtain a 
working copy of the game from official releases here: http://revolutionarygamesstudio.com/releases/
alternatively you can request a newer version from developers or compile the project yourself.

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage. 
And [AngelScript primer][asprimer] for scripting help.

### C++ Programmers 
To compile Thrive yourself, you will need to follow the [setup instructions][setupguide].

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

### Modellers, texture and GUI artists, and Sound Engineers
To work on the art assets you will want a working copy of the game.
You can find official releases here: http://revolutionarygamesstudio.com/releases/
alternatively you can request a newer version from developers, or compile the project yourself.
 
After you have obtained a working version of the game, you can place any new assets in the corresponding subdirectories:
bin/Data/Sound, bin/Data/Models, bin/Data/Materials and gui and the game will automatically detect up your new files, which you can then use in scripts.
An example of modifying a script to use your model would be to open scripts/microbe_stage/organelle_table.as with a text editor and 
find 'nucleusParameters.mesh = "nucleus.mesh";' that sets the model used by the nucleus and change that to your new model file.
Similarly you can find sections of the scripts that use other assets and replace the assets they use.
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

[blender_ogre_tutorial]: http://thrivegame.wikidot.com/blender-and-ogre-tutorial "Blender to ogre tutorial"
[asset_repository]: https://boostslair.com/svn/thrive_assets "Asset Repository"
[tortoiseSVN]: http://tortoisesvn.net/docs/release/TortoiseSVN_en/ "Tortoise SVN"
[styleguide]: doc/style_guide.md "Styleguide"
[setupguide]: doc/setup_instructions.md
[asprimer]: doc/angelscript_primer.md "AngelScript primer"
