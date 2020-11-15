What's this?
============

These are the setup instructions for working with and compiling Thrive.

Important Note: If you run into any trouble with the setup process, please
bring them up [on the forums](https://community.revolutionarygamesstudio.com/c/dev-help),
or if you are a team member you can ask on the development discord or open a github issue.

If you are a team member you can ask for help on the [Private
Developer
Forums](https://forum.revolutionarygamesstudio.com/c/programming)

You can also join and ask on our [community
discord](https://discordapp.com/invite/FZxDQ4H) please use the
#thrive-modding channel for that.

Thank you!

Prerequisites
=============

NOTE: since the move to Godot the setup process has changed a lot and
these instructions are not as battle tested as before, so if you have
issues please don't hesitate to bring them up.


Godot mono version
------------------

The currently used Godot version is __3.2.3 mono__. The regular version
will not work. You can download Godot here: https://godotengine.org/download/
if it is still the latest stable version. If a new version of Godot has
been released but Thrive has not been updated yet, you need to look
through the [previous Godot
versions](https://downloads.tuxfamily.org/godotengine/) to get the
right version.

Godot is self-contained, meaning that you just extract the downloaded
archive and run the Godot executable in it.


Git with LFS
------------

To clone the Thrive repository properly you need Git with Git LFS.

You need at least Git LFS version 2.8.0, old versions do not work.

On Linux and mac use your package manager to install git. Git lfs is
likely available as a package named `git-lfs`. If it is not install it
manually. After installing remember to run `git lfs install` in terminal.

On Windows install Git with the official installer from:
https://git-scm.com/download/win You can use this installer to also
install git lfs for you. After installing you need to run `git lfs install`
in command prompt. You'll also need to turn autocrlf on with the command
`git config --global core.autocrlf true`

If you previously had Git installed through cygwin, you must uninstall
that and install the official Windows version of Git. You may also
have to deleted all your cloned folders to avoid errors, and reboot
your computer to have everything detect that Git is now in a different
place.

If you haven't used git before you can read a free online book to learn
it here https://git-scm.com/book/en/v2 or if you prefer video learning
these two are recommended https://www.youtube.com/watch?v=SWYqp7iY_Tc
https://www.youtube.com/watch?v=HVsySz-h9r4


A development environment
-------------------------

You need a supported development environment for Godot with
mono. Note: it is possible to get by with just C# build tools, but
installing a development environment is the easier route.

Godot currently supports the following development environments:

- Visual Studio 2019
- Visual Studio Code
- JetBrains Rider
- MonoDevelop
- Visual Studio for Mac

### MonoDevelop

On Linux MonoDevelop and Jetbrains Rider are recommended. To get an up
to date version, first enable the mono repository:
https://www.mono-project.com/download/stable/ and then install the
following packages with your package manager: `mono-complete
monodevelop nuget`. Make sure it is a newer version of mono that comes
with msbuild. Fedora has mono in the official repo but it is too old
to work. If you are going to use Rider you don't need the monodevelop
package.

On Windows don't intall Mono or MonoDevelop, it will break things.

For a better experience with Godot, you can install the following
addon for MonoDevelop:
https://github.com/godotengine/godot-monodevelop-addin This is not
needed for basic usage, so you can skip if you can't figure out how to
install it.

### Jetbrains Rider

Jetbrains Rider is recommended for Thrive development on Linux. It is
available from: https://www.jetbrains.com/rider/

It has a Godot plugin which is easy to install. With Rider the
debugging experience is better than with MonoDevelop.

Rider requires mono to be installed similarly to MonoDevelop, so
follow those instructions on how to get mono setup.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/rider_godot_plugin.png" alt="rider godot plugin" width="600px">

For better experience make sure to install the Godot plugin for Rider.

### Visual Studio 2019

On Windows you can use Visual Studio 2019 to work on Thrive. You can
find download and setup instructions here:
https://docs.godotengine.org/en/stable/getting_started/scripting/c_sharp/c_sharp_basics.html#configuring-vs-2019-for-debugging

### Visual Studio Code

Note: Setting up Visual Studio Code with Linux is possible,
however it is recommended to use MonoDevelop instead

Visual Studio Code, not to be confused with Visual Studio, doesn't
come with build tools, so you'll need to install the build tools for
Visual Studio from here:
https://visualstudio.microsoft.com/downloads/?q=build+tools You will
need **at least** VS Build tools 2019 due to the version of C# used by
Thrive. During the installation process, make sure MSBuild tools is
listed under the installation details.

Go to https://dotnet.microsoft.com/download Under the .NET Core
section, click on _Download .NET Core SDK_ and run the installer.
Go back to the main download page and find
_All .NET Framework Downloads_ Choose version 4.7 and select the Developer Pack.

Open Visual Studio Code and go to the Extensions tab. Get the extensions
_C#_, _Mono Debug_, and _C# Tools for Godot_.

Open up a Project in Godot. On the top toolbar, go to Editor -> Editor Settings.
Scroll down on the left window until you find Mono. Click on Editor and set
External Editor to Visual Studio Code. Click on Builds and set Build Tool to
MSBuild (VS Build Tools).

If you want to setup live debugging with Godot follow the instructions here
https://docs.godotengine.org/en/stable/getting_started/scripting/c_sharp/c_sharp_basics.html#configuring-visual-studio-code-for-debugging


Building Thrive
===============

After downloading all the prerequisites you can now build Thrive.

Clone
-----

If you plan on contributing to Thrive fork it on github so you
have your own repository to work with. There is an in-depth guide
for working with forks
[here](https://gist.github.com/Chaser324/ce0505fbed06b947d962).

Next, use `git clone` to copy your fork to your computer. If you're
setting up Thrive just for testing or because you want to try in
development features you don't need to fork the project, and can clone
the main Thrive repository.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/terminal_git_clone.png" alt="termina running git clone">

Terminal showing git clone command. If you don't see the line with
"filtering content", then you don't have Git LFS working correctly.
If you don't have Github ssh key setup, you'll want to use a HTTPS URL
for cloning.

If you use the "download as zip" option on Github, it won't work. This
is because that option won't properly include the Git LFS files in it.

Note: a path with spaces in it MAY NOT WORK, so to avoid issues you
should clone to a folder like `~/projects` or `C:/projects`. Also, long
paths may cause issues on Windows. One additional potential problem is
non-English characters in the path name, for example if your path
includes "työpöytä", the setup will fail.

You should check at this point if
the image files in `Thrive/assets/textures` can be opened, if they
can't be opened and their file sizes are tiny, you don't have Git LFS
properly installed.

For devs working directly with the Thrive repository switch to a feature
branch or create one when working on new features. For example `git checkout godot`.
This keeps the main branch clean as other branches can be merged through
pull requests on github which is the recommended way to get your code into
Thrive.

Setup
-----

### Project import

Now open your installed Godot with mono. It should open with a project
manager window. From this window select the option to import a
project.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/godot_import_button.png" alt="godot import button" width="550px">

Godot project manager with the import button highlighted. If you haven't
used Godot before the list of projects is most likely blank, so please ignore
the demo project in this screenshot.

Use that option and input the path to the folder you cloned
Thrive in and import the project.godot file. Like this:

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/godot_project_import.png" alt="godot project import" width="550px">

Clicking on "import & edit" will immediately open the project in Godot.
So next time you open Godot you will see Thrive on the list of projects,
double click it to open it.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/project_in_godot_pm.png" alt="godot project manager" width="550px">

Thrive on the project list

Now you should let the Godot editor sit for some time until it is done
checking and importing all the assets. If the asset import fails it is
probably because you didn't have Git LFS installed properly. If that
happens delete the Thrive folder you cloned and go back to the Git LFS
install step, after doing it again try cloning again.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/thrive_open_in_godot.png" alt="thrive in godot" width="720px">

If everything went fine you should now see Godot editor looking like in this image.

On the top toolbar, go to Editor -> Editor Settings.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/godot_editor_settings_location.png" alt="godot editor settings">

Scroll down on the left window until you find the Mono section.
Click on Editor. Set External Editor to your development environment.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/godot_external_editor_settings.png" alt="godot external editor" width="550px">

Here selected IDE is Rider.

Click on Builds under Mono and set Build Tool to your compiler. If you have build errors,
check if this is setup properly.

On Linux it is required to use mono as the build tool as selected in
Godot editor settings. The option to build with dotnet is creates
broken releases. Like this:

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/godot_build_tool_option_linux.png" alt="godot linux build tool" width="550px">

Even if you do not use the Godot script editor, Godot automatically opens some files and replaces the spaces with tabs.
To stop Godot from messing with you files, go to Text Editor -> Indent and set Type to spaces

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/godot_editor_use_spaces.png" alt="set intend to spaces" width="550px">

### C# packages

Thrive uses some external C# packages which need to be restored before
compiling.

On Linux, or if you're using Visual Studio Code, open a terminal to
the thrive folder and run the following
command: `nuget restore` it should download the missing nuget
packages. You may need to rerun this command when new package
dependencies are added to Thrive. Note: if you use MonoDevelop it
*might* automatically restore missing packages using nuget when
compiling the game within MonoDevelop.


On Windows you should use Visual Studio to restore the packages. To do
this open `Thrive.sln` in the Thrive folder. The package restore might
automatically happen if you compile the solution. If it doesn't please
refer to this page on how to restore the nuget packages with Visual
Studio:
https://docs.microsoft.com/en-us/nuget/consume-packages/package-restore

If you have nuget in path or you use the Visual Studio command prompt
you should also be able to restore the packages by running `nuget
restore` in the Thrive folder.

Note: dotnet restore command should be avoided as it can break things.

Compiling
---------

Now you should be able to return to the Godot editor and hit the build
button in the top right corner. If that succeeds then you should be
good to go.

You can also compile from your development
environment (and not the Godot editor) to see warnings and get
highlighting of errors in the source code. However running the game
from Visual Studio is a bit complicated.

If the compile fails with a bunch of `Godot.something` or `Node` not
found, undefined references, you need to compile the game from the
Godot editor to make it setup the correct Godot assembly
references. After that compiling from an external tool should work.

From MonoDevelop you can use the plugin mentioned before, that adds a
toolbar with a button to launch the game. To do that open `Thrive.sln`
with MonoDevelop and in the new toolbar select the options `Thrive -
Launch` and `Tools` then you can hit the play button to the left of
the dropdown options. This should compile and start Thrive so that
breakpoints set in MonoDevelop work.

From Rider you can compile the game from the top right menu bar by
selecting the `Player` target (with the Godot icon). That target
should automatically appear once you install the Godot plugin
(https://plugins.jetbrains.com/plugin/13882-godot-support).

With that plugin you can run the game from Godot (once you have ran once
from Godot editor so that it sets up things), using these toolbar buttons
and options:

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/rider_debugging_buttons.png" alt="rider debug toolbar">

If it doesn't automatically appear you should be able to manually add it with the
configuration editor.

Done
----

If the build in Godot editor succeeded, you should be now good to go.

You can run the game by pressing the play button in the top right of
the Godot editor or by pressing F5. Additionally if you open different
scenes in the editor you can directly run the current scene to skip
the main menu.

If it didn't work you can try these:

- Thrive community forums
- Thrive developer forums
- Thrive community discord
- Thrive developer discord (you can only access this if you are a team member)

to get help. Links to the forums and community discord
are at the top of this document.

Git LFS limitations
-------------------

Due to current [Git LFS server](https://github.com/Revolutionary-Games/ThriveDevCenter)
limitations you can't commit changes to files tracked by Git LFS if
you are not a team member. If that is needed for a PR please ask some
team member to commit the assets for you.

For information on committing to the LFS repository you can read this
wiki page https://wiki.revolutionarygamesstudio.com/wiki/Git_LFS

Optional downloads
------------------

In addition to the following optional downloads you need to have Godot
in your PATH for the scripts to find it. To do this create a link /
rename the Godot editor executable to just `godot` or `godot.exe` if
you are on Windows. Then you need to either add the folder where that
executable is to your system PATH or move the executable (along the
other Godot resources it needs) to a path that is already in PATH.

### Ruby

Ruby is needed for the scripts to package the game, and for the
code style checks. So while it is not necessary to download this
it is highly recommended.

On Linux and mac you probably already have this, but if not, use a
package manager to install it.

On windows it is recommended to use RubyInstaller, version 2.4 or
newer, when installing make sure to also install the MSYS option in
order to be able to install gems.

After installing ruby open a terminal / command prompt and run:

```sh
gem install os colorize rubyzip json sha3 httparty parallel nokogiri
```

On Linux you might need to run the command with `sudo`.


If you have trouble installing sha3 on windows: make sure you have
ruby 2.4 or newer installed with ruby installer for windows. Then run
`ridk install` and try all of the options. The third option at least
should reinstall all the ruby development tools, including gmp, which
is needed for sha3. After that your ruby native extension build tools
should be installed and the gem installation should work.

If it still doesn't work run `ridk exec pacman -S gmp-devel` and then
run `ridk install` again.


### 7zip

7zip is needed for the game release script, so if you're not packaging
the game for release you don't need this.

On Linux and mac, use your package manager to install, it is probably
named `p7zip`.

On Windows download the [official](http://www.7-zip.org/download.html)
installer release. After installing, add the installed folder (where
`7z.exe` is) to PATH environment variable. Confirm by running `7z.exe`
in command prompt or powershell, it should print 7zip version info and
command line usage.


Linter
------

Thrive uses automatic formatting of source code to keep style as
consistent as possible. It is highly recommended you install
this linter to check your code formatting before submitting a
pull request.

## NodeJS
First install [NodeJS](https://nodejs.org/en/download/). If you are on
Linux you should use your OS's package manager to install nodejs.

After installing nodejs install the linter packages with this command:
```sh
npm install -g jsonlint
```

if that doesn't work run:
```sh
sudo npm install -g jsonlint
```

## Jetbrains tools

Download from:
https://www.jetbrains.com/resharper/download/#section=commandline
unzip and add to PATH. Currently used version is:
JetBrains.ReSharper.CommandLineTools.2020.2

NOTE: there is more documentation on the install process here:
https://www.jetbrains.com/help/resharper/InspectCode.html

On Linux you need to install the dotnet runtime for them to work. On
Fedora this can be done with: `sudo dnf install dotnet-runtime-3.1`

## Localization tools

If you are planning to do anything that would require translation, or
simply to translate the game into your locale language you may need
a few more tools.

**NOTE: if you are simply planning to edit or add a new localization, Poedit is
enough. You can find more information on how to translate the game with poedit
 [here](working_with_translations.md#Translating-the-game-into-a-new-language).**

### Python 3

The tool used to extract strings from the game files is using
[Python 3](https://www.python.org/downloads).
You'll need it if you are planning to add or edit strings in the game.

NOTE: Linux users should already have it installed.
You can use the command `python --version` to make sure you have it. On some distros 
the command is named `python3`, in which case `pip` maybe named `pip3`.
If you don't have Python, you can use the package manager of your distribution to 
install the `python3` package.

### Babel and Babel_thrive

Babel and its extension [Babel_thrive](https://github.com/Revolutionary-Games/pybabel-godot-thrive)
are the tools used for extracting strings from the game files.
Just like Python, you'll want to download these if you are planning
to add or edit strings into the game.

You can quickly install these by using the command 
`pip install Babel Babel-Thrive` or 
`pip3 install Babel Babel-Thrive`
if you have Python installed. On Linux you need to use the `--user`
flag to get the `pybabel` command to work, installing with sudo won't
work.

### Poedit (optional)

[Poedit](https://poedit.net/) is a free .pot and .po file editor that may
make your life easier when working on translation files.

It is needed to create new .po files without using the command line tools.

NOTE: Poedit will complain about translation format since it was made to
directly use text as keys. Those can be ignored.

### Gettext tools

If you want to run the translation scripts and checks, you need the gettext command 
line tools. They are also an alternative to using Poedit, with the gettext tools you can just
use them and a plain text editor to work on translations.

On Windows you can download precompiled versions of the tools. You will likely need to extract
them and then add the folder you extracted them in to your PATH for them to be found.

On Linux use your package manager to install the `gettext` package.

## Running the Format Checks

When you are getting ready to commit you should run `ruby
check_formatting.rb` in order to automatically run all of the
formatting tools. Make sure that that script doesn't report any errors
before committing.

When running the script like that it can take a long time to run. See
the pre-commit hook section for how to speed things up.

Pre-commit hook
---------------

On Linux you can enable a pre-commit hook to automatically run the
formatting checks before each commit to avoid accidentally committing
code with formatting issues. To install the hook run the following
script:

```sh
./install_git_hooks.rb
```

The hook has the advantage that it will only run the checks on the
files staged for commit saving many minutes of time. You can manually
emulate this by creating a file in the Thrive folder called
`files_to_check.txt` with one relative path per line specifying which
files to check.

## Additional Tips

### Troubleshooting (Windows)

If Godot still can't build the full game after following the
instructions, you should verify that it's using the proper toolset. Go
to Editor > Editor Settings > Builds under Mono in the panel on the
left. For VS2019, you should select MSBuild (VS Build Tools) for the
build tool option, if it isn't already.

### Build problems with unsupported C# version

If the build fails with errors about unsupported C# language version,
you need to update your VS build tools, if you are on Windows, or
mono, if on Linux. Note that you should use the official mono repo on
Linux to get the latest version of mono.

### Cleaning Godot

Your locally cloned Thrive version may get messed up from time to time.
[Here are the steps to fix it.](https://wiki.revolutionarygamesstudio.com/wiki/Cleaning_Local_Thrive_Version)

### Translating the game

You can find information about how to translate the game on the 
[Working with translation page](working_with_translations.md).
