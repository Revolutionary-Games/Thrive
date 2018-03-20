What's this?
============

This is the setup instructions for compiling Thrive.

Important Note: If you run into any trouble with the setup scripts, please 
bring them up on the development slack or open a github issue.

If you are a team member you can ask help on the forums:
    [Private Developer Forums](http://forum.revolutionarygamesstudio.com/)

Thank you!

Prerequisites
=============

10 GiB of disk space and about 60 minutes of compile time on a good CPU.

Follow the Leviathan Engine Prequisites installation instructions
[here](https://leviathanengine.com/doc/develop/Documentation/html/dc/d9e/prerequisites.html).

Important: you should read the whole document before starting as many
common pitfalls and issues have fixes given after later in the document. Also DO
NOT SKIP ANY STEPS otherwise you will initially fail and have to clear
the caches which is the easiest to do by just deleting the entire
folder and starting again. When you are done with the prerequisites
page return here instead of continuing to the building Leviathan page
which is irrelevant for Thrive.

Building Thrive
===============

Clone
-----

Open a terminal on linux or a command prompt (or powershell) on windows to a folder where you want to place the Thrive folder.

Note: a path with spaces in it WILL NOT WORK, so to avoid issues you
should choose a folder like `~/projects` or `C:/projects`. Also long
paths don't work on Windows as the setup needs the path in which it is
ran to be less than 90 characters, so choose run git clone in
`C:\projects` so that you end up the thrive folder being
`C:\projects\Thrive`.

Windows tip: shift right-click in a folder and select "Open command
prompt here" or "Open powershell here" to open a cmd window to the
folder

And now run

```
git clone https://github.com/Revolutionary-Games/Thrive.git
cd Thrive
```

To get the Thrive repository cloned.

For devs working on engine changes: switch to the engine_refactor
branch with `git checkout engine_refactor`. Or to another branch you
want to work on. This keeps the main branch clean as other branches
can be merged through pull requests on github which is the recommended
way to get your code into Thrive.

If you aren't on the team (have push access on github) create a fork
on github and use the url of that instead of the one above.


Running Setup
-------------

Now you can run the setup script with (make sure you have setup prerequisites):

```
ruby SetupThrive.rb thrive
```

The last part of the command (thrive) is the svn username. If you have
an svn assets username (ask on slack) use it instead of "thrive" here.

When asked to login to svn type in your svn password. Or if you are
using "thrive" as username type in "thrive" as the password as well.

Note: if you have a small amount of ram you may want to limit the
setup script to only use a few CPU cores for building or don't want to
dedicate all of your CPU. To do this add `-j 2` to the end of the
setup command with the number being the number of cores you want to
use, the default is to use all. Note: this may not work for the
dependencies (and that needs fixing)

Done
----

If the setup script succeeds then congratulations you have just
successfully built Thrive and can start working.

If it didn't work you can try these:
- [Leviathan Troubleshoothing tips](https://leviathanengine.com/doc/develop/Documentation/html/dc/dca/compiling_leviathan.html#compile_troubleshooting)
- Thrive forums
- Thrive discord
- Thrive developer chat on slack

to get help

Here are some quick tips for working on thrive:

- [Scripting
  documentation](https://leviathanengine.com/doc/develop/Documentation/html/d0/db5/angelscript_main.html)
- On Windows: when building Thrive in visual studio select
  `RelWithDebInfo` configuration (instead of Debug). Otherwise the build
  may fail. And make sure "Thrive" is the startup project to run it in the debugger.
- Whenever you **change the assets** or scripts you need to run cmake. See
  [this](https://leviathanengine.com/doc/develop/Documentation/html/df/d4e/tutorial1.html#tutorial1recompiling)
  for more info

Forking
-------

If you are just starting out and you don't have write access to the
Thrive repository yet you will need to create a fork. Use the fork
button on Github on the main repository page. This will give you a
fork (for example my fork is https://github.com/hhyyrylainen/Thrive).

Then you can add that to your git config in the thrive folder with this command:

```
git remote add fork https://github.com/YOURUSERNAMEHERE/Thrive.git
```

Then after committing your changes (git-cola is recommended or another
graphical tool for reviewing the exact lines you have changed, but you
can also commit on the command line) you can publish them to your fork
with (assuming you used the master branch, when working with the main
thrive repository you MUST create a different branch but when working
with a fork that isn't required):

```
git push fork master
```

Now you can open a pull request by visiting the Github page for your
fork.
