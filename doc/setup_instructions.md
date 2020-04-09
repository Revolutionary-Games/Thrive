What's this?
============

These are the setup instructions for compiling Thrive.

Important Note: If you run into any trouble with the setup process, please
bring them up [on the forums](https://community.revolutionarygamesstudio.com/c/dev-help),
or if you are a team member you can ask on the development discord or open a github issue.

If you are a team member you can ask for help on the [Private
Developer
Forums](http://forum.revolutionarygamesstudio.com/c/programming)

You can also join and ask on our [community
discord](https://discordapp.com/invite/FZxDQ4H) please use the
#thrive-modding channel for that.

Thank you!

Prerequisites
=============

NOTE: since the move to Godot the setup process has changed a lot and
these instructions are not as battle tested as before, so if you have
issues please don't hesitate to bring them up.


Godot with mono
---------------

The currently used Godot version is __3.2 mono__. You can find it on
the Godot download page: https://godotengine.org/download/ if it is
still the latest stable version. If a new version of Godot has been
released but Thrive has not been updated yet, you need to look through
the [previous Godot
versions](https://downloads.tuxfamily.org/godotengine/) to get the
right version.


Git with LFS
------------

To clone the Thrive repository properly you need Git with Git LFS.

You need at least Git LFS version 2.8.0, old versions do not work.

On Linux and mac use your package manager to install git. Git lfs is
likely available as a package named (git-lfs). If it is not install it
manually. After installing remember to run `git lfs install` in terminal.

On Windows install Git with the official installer from:
https://git-scm.com/download/win You can use this installer to also
install git lfs for you. Just don't forget to run `git lfs install`
in command prompt afterwards.

If you previously had Git installed through cygwin, you must uninstall
that and install the official Windows version of Git. You may also
have to deleted all your cloned folders to avoid errors, and reboot
your computer to have everything detect that Git is now in a different
place.


A development environment
-------------------------

You need a supported development environment for Godot with
mono. Note: it is possible to get by with just C# build tools, but
installing a development environment is the easier route.

On Linux MonoDevelop is recommended. To get an up to date version,
first enable the mono repository:
https://www.mono-project.com/download/stable/ and then install the
following packages with your package manager: `mono-complete
monodevelop nuget`

For a better experience with Godot, you can install the following
addon for MonoDevelop:
https://github.com/godotengine/godot-monodevelop-addin This is not
needed for basic usage, so you can skip if you can't figure out how to
install it.

On Windows you should be able to install Visual Studio 2019 with the
desktop development featureset, that should have everything needed for
C# development. Please be patient with this as this is going to take
tens of minutes to install and take a lot of disk space.

It might also be possible to make things work with Visual Studio Code
as well, but there are no instructions for that. Note: that Godot
requirements say that Visual Studio build tools are enough but they
may not include nuget, which is needed, so if you go that route you
may need to manually install nuget.

### Visual Studio Code

To setup Visual Studio Code  work with Godot, you'll need
Visual Studio Code. You can install Visual Studio Code from here:
https://code.visualstudio.com/

Note: Setting up Visual Studio Code with Linux is possible,
however it is recommended to use MonoDevelop instead

Next, install Build Tools for Visual Studio here:
https://visualstudio.microsoft.com/downloads/?q=build+tools During the
installation process, make sure MSBuild tools is listed under
Installation details.

Go to https://dotnet.microsoft.com/download Under the .NET Core
section, click on _Download .NET Core SDK_ and run the installer.
Go back to the main download page and find
_All .NET Framework Downloads_ Choose version 4.7 and select the Developer Pack.

Open Visual Studio Code and go to the Extensions tab. Get the extensions
_C#_, _Mono Debug_, and _godot-tools_.

Open up a new Godot Project in the Godot editor. On the top toolbar,
go to Editor -> Editor Settings. Scroll down on the left window until 
you find Mono. Click on Editor and set External Editor to Visual
Studio Code. Click on Builds and set Build Tool to MSBuild (VS Build Tools).

If you want to setup live debugging with Godot, go to the top toolbar,
go to Project -> Project Settings. Scroll down on the left window until
you find Mono. Click on Debugger Agent. When you want to use the debugger,
turn the _Wait for Debugger_ setting on. Set the _Wait Timeout_ to how
many milliseconds you want Godot to wait for your debugger to connect.
Setting it to 15000 is recommended. Copy the port number and open up
Visual Studio Code. Make sure Visual Studio Code has the Godot project 
folder open. Go to the debug tab and click on
_create a launch.json file_. Select C# Mono from the dropdown menu.
When the _launch.json_ file is automatically opened, change the port
number to the number you copied previously. Save the file.
On the Debug tab, switch the Run setting from Launch to
Attach. Whenever you want to debug, make sure _Wait for Debugger_ is
turned on in Godot, run the project, and run the debugger in Visual 
Studio Code.

Optional
--------

The following prerequisites are not absolutely necessary for working
on Thrive, so you can skip them if you want to get started faster or
have issues with them.


### Ruby

On Linux and mac you probably already have this, but if not, use a
package manager to install it.

On windows it is recommended to use RubyInstaller, version 2.4 or
newer, when installing make sure to also install the MSYS option in
order to be able to install gems.

After installing ruby open a terminal / command prompt and run:

```sh
gem install os colorize rubyzip json sha3
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

On Linux and mac, use your package manager to install, it is probably
named `p7zip`.

On Windows download the [official](http://www.7-zip.org/download.html)
installer release. After installing, add the installed folder (where
`7z.exe` is) to PATH environment variable. Confirm by running `7z.exe`
in command prompt or powershell, it should print 7zip version info and
command line usage.


Linters
-------

Thrive uses automatic formatting of source code to keep style as
consistent as possible.

Note: the following tools is not currently used

For this reason you need to install some
additional tools.

[NodeJS](https://nodejs.org/en/download/). If you are on Linux you
should use your OS's package manager to install nodejs.

After installing nodejs install the linter packages with this command:
```sh
npm install -g jsonlint
```

When you are getting ready to commit you should run `ruby
check_formatting.rb` in order to automatically run all of the
formatting tools. Make sure that that script doesn't report any errors
before committing.


Building Thrive
===============

Make sure you installed the non-optional prerequisites first!

Clone
-----

Open a terminal on Linux or a command prompt (or powershell) on
Windows to a folder where you want to place the Thrive folder.

Note: a path with spaces in it MAY NOT WORK, so to avoid issues you
should choose a folder like `~/projects` or `C:/projects`. Also, long
paths may cause issues on Windows. One additional potential problem is
non-English characters in the path name, for example if your path
includes "työpöytä", the setup will fail.

Windows tip: shift right-click in a folder and select "Open command
prompt here" or "Open powershell here" to open a cmd window to the
folder.

And now run:

```
git clone https://github.com/Revolutionary-Games/Thrive.git
cd Thrive
```

To get the Thrive repository cloned. You should check at this point if
the image files in `Thrive/assets/textures` can be opened, if they
can't be opened and their file sizes are tiny, you don't have Git LFS
properly installed.

For devs working on new features: switch to a feature branch or create
one. For example `git checkout godot`. This keeps the main
branch clean as other branches can be merged through pull requests on
github which is the recommended way to get your code into Thrive.

If you aren't on the team (you don't have push access on github)
create a fork on github and use the url of that instead of the one
above for cloning so that you can push your changes.

Setup
-----

### Project import

Now open your installed Godot with mono. It should open with a project
manager window. From this window select the option to import a
project. Use that option and input the path to the folder you cloned
Thrive in and import the project.godot file.

Now you should see Thrive on the list of projects, double click it to
open it.

Now you should let the Godot editor sit for some time until it is done
checking and importing all the assets. If the asset import fails it is
probably because you didn't have Git LFS installed properly. If that
happens delete the Thrive folder you cloned and go back to the Git LFS
install step, after doing it again try cloning again.

At this point you might want to go through the Godot editor settings
and select the development environment you installed as your external
code editor.

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

For developing Thrive you can also compile from your development
environment (and not the Godot editor) to see warnings and get
highlighting of errors in the source code. However running the game
from Visual Studio is a bit complicated.

From MonoDevelop you can use the plugin mentioned before, that adds a
toolbar with a button to launch the game. To do that open `Thrive.sln`
with MonoDevelop and in the new toolbar select the options `Thrive -
Launch` and `Tools` then you can hit the play button to the left of
the dropdown options. This should compile and start Thrive so that
breakpoints set in MonoDevelop work.

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

Forking
-------

If you are just starting out and you don't have write access to the
Thrive repository yet, you will need to create a fork. Use the fork
button on Github on the main repository page. This will give you a
fork under your github account (for example my fork is https://github.com/hhyyrylainen/Thrive).

Then you can add that to your git config in the thrive folder with this command:

```
git remote add fork https://github.com/YOURUSERNAMEHERE/Thrive.git
```

Then after committing your changes (git-cola is recommended or another
graphical tool for reviewing the exact lines you have changed, but you
can also commit on the command line) you can publish them to your fork
with (assuming you used the master branch, when working with the main
thrive repository you MUST create a different branch but when working
with a fork that isn't required, but still strongly recommended as
making multiple pull requests with one branch is messy):

```
git push fork master
```

Now you can open a pull request by visiting the Github page for your
fork.

There is an in-depth guide for working with forks
[here](https://gist.github.com/Chaser324/ce0505fbed06b947d962).

Note: that due to current Git LFS limitations you can't commit changes
to files tracked by Git LFS if you are not a team member. If that is
needed for a PR please ask some team member to commit the assets for
you.
