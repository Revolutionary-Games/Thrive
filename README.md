Thrive
======

This is the code repository for Thrive. For more information, visit
[Revolutionary Games' Website](https://revolutionarygamesstudio.com/).

Overview
----------------
Repository structure:
- assets: This folder contains all the assets such as models and other binaries. The big files in this folder use [Git LFS](https://git-lfs.github.com/) in order to keep this repository from bloating. You need to have Git LFS installed to get the files.
- doc: Documentation files. Contains style guide, engine overview and other useful documentation.
- scripts: AngelScript scripts that contain part of the codebase. Scripts are used for easier development and code here can then later be transferred to the C++ base for performance. 
- src: The C++ code base containing the common helper classes and gameplay code moved to C++.
- test: Contains tests that will ensure that core parts work correctly. These are currently really lacking.

Getting Involved
----------------
Depending on what you want to contribute, you need to take different steps
to get your development environment set up.

Read the [contribution guidelines](CONTRIBUTING.md) first. If you need
help please ask [on our
forums](https://community.revolutionarygamesstudio.com/c/25-dev-help).

There are also other useful documents in the doc folder not mentioned here.

### Script Authors
If you only want to modify the AngelScript scripts, you can obtain a 
working copy of the game from official releases [here][releasespage],
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
You can find official releases [here][releasespage],
alternatively you can request a newer version from developers, or compile the project yourself.

After you have obtained a working version of the game, you can place any new assets in the corresponding subdirectories:
bin/Data/Sound, bin/Data/Models, or bin/Data/Textures. The game will automatically detect your new files,
which you can then use in scripts.
An example of modifying a script to use your model would be to open scripts/microbe_stage/organelle_table.as with a text editor and 
find 'nucleusParameters.mesh = "nucleus.fbx";' that sets the model used by the nucleus and change that to your new model file.
Similarly you can find sections of the scripts that use other assets and replace the assets they use.
If you are replacing an imported asset file (file extension `.asset`) you need to use the [import tool][importtutorial] to convert 
it before the game can use the file. Also if you are adding a new asset that needs to be in converted form (models, textures, 
shaders, etc.) you need to convert the file before using.

If you are truly uncomfortable with editing scripts you can simply try stealing the names of existing assets. For example 
going into the sound subdirectory and stealing the name "microbe-theme-1.ogg" by renaming your new sound-file to that and the 
game will then play that sound instead.

To contribute assets you can contact a developer and provide that person with your assets and the developer can add the assets to
the official repository. It will at a later time be possible to [commit](https://wiki.revolutionarygamesstudio.com/wiki/Git_LFS) to
Git LFS server yourself, currently it is limited to only Thrive developers. Note that you must have Git LFS installed.

Extra note for modellers:
You should export your model in FBX format from blender or other
modelling software you use, and then use the [import
tool][importtutorial] to convert the model to an usable form. If you
are importing a model with a skeleton or morph animations you'll need
a special import configuration file.

[releasespage]: https://revolutionarygamesstudio.com/releases/
[styleguide]: doc/style_guide.md "Styleguide"
[setupguide]: doc/setup_instructions.md
[asprimer]: doc/angelscript_primer.md "AngelScript primer"
[importtutorial]: https://wiki.revolutionarygamesstudio.com/wiki/How_to_Import_Assets "How to import assets"
