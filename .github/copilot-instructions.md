# Review Instructions

When reviewing code of a PR pay most attention to the following: logic flow of the code to check for logical errors, typos in code identifiers or comments, and code efficiency (Thrive is a game so avoiding temporary memory allocations as much as possible is important; persistent, reused data containers should be preferred).

Automated CI checks ensure code compiles and is formatted correctly (except for shader files, for those check they match the style of existing shader files in the shaders folder). Translation files in the locale folder don't need to be checked, other than `en.po` to see if the English text is grammatically fine and has no typos, as they also have automated checks to ensure no missing text. So don't bother reading other `.po` files than the `en.po` as they are not required to be reviewed.

The Thrive project style guide is in the doc folder at path `doc/style_guide.md` read that guide before reviewing a PR to make sure the PR follows the rules set out in the style guide, this ensures the code conforms to existing Thrive codebase and keeps code quality high.

A few specific common mistakes to check the PR doesn't contain:
- Changing `visible = false` line of a node in a .tscn file to be `true` or removing line containing `false` is often a mistake as it makes a node visible by default which is often done for testing purposes but then should not be committed most of the time.
- Adding any new C# file, for example `ExampleClass.cs`, requires also a new `uid`file matching it: `ExampleClass.cs.uid`. If it is missing then the PR author has forgotten to open Godot which will automatically generate such files if missing.
