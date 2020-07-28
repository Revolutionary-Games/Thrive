Thrive
======

This is the code repository for Thrive. For more information, visit
[Revolutionary Games' Website](https://revolutionarygamesstudio.com/).

### Build Status [![CircleCI](https://circleci.com/gh/Revolutionary-Games/Thrive.svg?style=svg)](https://circleci.com/gh/Revolutionary-Games/Thrive)
### Patreon [![Patreon](https://img.shields.io/badge/Join-Patreon-orange.svg)](https://www.patreon.com/thrivegame)

Overview
----------------
Repository structure:
- assets: This folder contains all the assets such as models and other binaries. The big files in this folder use [Git LFS](https://git-lfs.github.com/) in order to keep this repository from bloating. You need to have Git LFS installed to get the files. Some better editable versions of the assets are stored in a separate [repository](https://github.com/Revolutionary-Games/Thrive-Raw-Assets).
- [doc: Documentation files.](/doc) Contains style guide, engine overview and other useful documentation.
- simulation_parameters: Contains JSON files as well as C# constants for tweaking the game.
- scripts: Utility scripts for Thrive development
- src: The core of the game written in C# as well as Godot scenes.
- test: Contains tests that will ensure that core parts work correctly. These don't currently exist for the Godot version.

Getting Involved
----------------
Depending on what you want to contribute, you need to take different steps
to get your development environment set up.

Read the [contribution guidelines](CONTRIBUTING.md) first. If you need
help please ask [on our
forums](https://community.revolutionarygamesstudio.com/c/dev-help).

There are also other useful documents in the [doc](doc) folder not mentioned here.

If you have game development skills, you can apply to the team
[here](https://revolutionarygamesstudio.com/application/).

### Programmers 
Thrive is written in C# with a few helper scripts written in ruby. In
order to work on the C# you need to compile Thrive yourself. You can
find instructions for how to do that in the [setup
instructions][setupguide].

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

Binary files should be committed using [Git LFS][lfs].

### Modellers, texture and GUI artists, and Sound Engineers
To work on the art assets you will want to install Godot and work on
the project files with it. Instructions for that are the same as for
programmers: [setup instructions][setupguide].

Alternatively some art assets can be worked on without having a
working copy of the Godot project, but then you need to rely on other
artists or programmers to put your assets in the game.

You should familiarize yourself with the Godot [Asset
pipeline](https://docs.huihoo.com/godotengine/godot-docs/godot/tutorials/asset_pipeline/_asset_pipeline.html).

To contribute assets you can contact a developer and provide that
person with your assets and the developer can add the assets to the
official repository. It will at a later time be possible to
[commit][lfs] to Git LFS server yourself, currently it is limited to
only Thrive developers. Note that you must have Git LFS installed for
this to work. Any artists on the team should preferrably modify the
project in Godot themselves and commit the assets using [Git
LFS][lfs].

Extra note for modellers:
There are extra instructions for how to import models here: [import tool][importtutorial]


[releasespage]: https://revolutionarygamesstudio.com/releases/
[styleguide]: doc/style_guide.md "Styleguide"
[setupguide]: doc/setup_instructions.md
[asprimer]: doc/angelscript_primer.md "AngelScript primer"
[importtutorial]: https://wiki.revolutionarygamesstudio.com/wiki/How_to_Import_Assets "How to import assets"
[lfs]: https://wiki.revolutionarygamesstudio.com/wiki/Git_LFS
