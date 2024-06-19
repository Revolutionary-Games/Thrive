Thrive
======

This is the code repository for Thrive. For more information, visit
[Revolutionary Games' Website](https://revolutionarygamesstudio.com/).

### [Build Status](https://dev.revolutionarygamesstudio.com/ci/1)
### Patreon [![Patreon](https://img.shields.io/badge/Join-Patreon-orange.svg)](https://www.patreon.com/thrivegame)

[![Thrive on Steam](https://img.shields.io/badge/-Thrive%20on%20Steam-blue)](https://store.steampowered.com/app/1779200/Thrive/) [<img height='20' alt='Thrive on Itch.io' src='https://static.itch.io/images/badge-color.svg'>](https://revolutionarygames.itch.io/thrive)

[![Community Forums](https://img.shields.io/badge/-Community%20Forums-%239cf)](https://community.revolutionarygamesstudio.com/)
<a href="https://translate.revolutionarygamesstudio.com/engage/thrive/">
<img src="https://translate.revolutionarygamesstudio.com/widgets/thrive/-/thrive-game/svg-badge.svg" alt="translation status" />
</a>
[![Developer Wiki](https://img.shields.io/badge/-Developer%20Wiki-red)](https://wiki.revolutionarygamesstudio.com/)
[![Discord](https://discord.com/api/guilds/228300288023461893/widget.png)](https://discord.gg/FZxDQ4H)

<br>
<img src="https://randomthrivefiles.b-cdn.net/screenshots/github_screenshot_1.png" alt="game screenshot" width="900px">

Overview
--------

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

Read the [contribution guidelines](CONTRIBUTING.md) and [code of conduct](CODE_OF_CONDUCT.md) first. If you need
help please ask [on our
forums](https://community.revolutionarygamesstudio.com/c/dev-help).

There are also other useful documents in the [doc](doc) folder not mentioned here.

If you have game development skills, you can apply to the team
[here](https://revolutionarygamesstudio.com/application/).

If you'd like to translate the game to your language, you can find the relevant information [here](doc/working_with_translations.md).

The planning board contains all issues and pull requests grouped
by their priority and status. It can be found [here](https://github.com/orgs/Revolutionary-Games/projects/2).

<br>
<img src="https://randomthrivefiles.b-cdn.net/screenshots/github_screenshot_2.png" alt="game screenshot" width="900px">

### Programmers 
Thrive is written in C#. In order to work on the C# you need to compile Thrive yourself. 
You can find instructions for how to do that in the [setup instructions][setupguide]. And 
if you've never used Godot before please read [learning Godot][learninggodot]. This repository
also contains a few helper scripts written in C# for working on the game. These can be ran
with dotnet: `dotnet run --project Scripts -- help`

Be sure to have a look at the [styleguide][styleguide],
both for guidelines on code formatting and git usage.

Binary files should be committed using [Git LFS][lfs].

### Modellers, texture and GUI artists, and Sound Engineers
To work on the art assets you will want to install Godot and work on
the project files with it. Instructions for that are the same as for
programmers: [setup instructions][setupguide]. And if you've never
used Godot before please read [learning Godot][learninggodot].

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

### Miscellaneous

The history for this repository has been slightly cleaned up to remove
large old binary files that were added before we used Git LFS. The original history can be found here: [original_master][originalmaster]

[releasespage]: https://revolutionarygamesstudio.com/releases/
[styleguide]: doc/style_guide.md "Styleguide"
[setupguide]: doc/setup_instructions.md
[asprimer]: doc/angelscript_primer.md "AngelScript primer"
[importtutorial]: https://wiki.revolutionarygamesstudio.com/wiki/How_to_Import_Assets "How to import assets"
[lfs]: https://wiki.revolutionarygamesstudio.com/wiki/Git_LFS
[learninggodot]: doc/learning_godot.md
[originalmaster]: https://github.com/Revolutionary-Games/Thrive/tree/original_master
