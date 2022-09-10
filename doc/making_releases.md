Making Releases
===============

The repository contains a script called `dotnet run --Scripts --
package` which handles the release making process.

As long as you have the prerequisites installed and the game runs from
the editor, running the script should work.

NOTE: it is untested on Windows

After the script runs it places the created releases in the `builds`
folder.

## Icon
For Windows icon see: 
https://docs.godotengine.org/en/3.2/getting_started/workflow/export/changing_application_icon_for_windows.html

## Updating release info

For Windows executables some version info is embedded from
export_presets.cfg, which needs to be manually kept up to date.
