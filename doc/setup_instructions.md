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

Building Thrive
===============

Clone
-----

Open a terminal on linux or a command prompt (or powershell) on windows to a folder where you want to place the Thrive folder.

Note: a path with spaces in it may or may not work, so to avoid issues
you should choose a folder like `~/projects` or `C:/projects`.

Windows tip: shift right-click in a folder and select "Open command
prompt here" or "Open powershell here" to open a cmd window to the
folder

And now run

```
git clone https://github.com/Revolutionary-Games/Thrive.git
cd Thrive
```

To get the Thrive repository cloned.

For devs: switch to the engine_refactor branch with `git checkout
engine_refactor` or another branch you want to work on.


Running Setup
-------------

Now you can run the setup script with:

```
ruby SetupThrive.rb thrive
```

If you have an svn assets username use it instead of "thrive" here.

When asked to login to svn type in your svn password. Or if you are
using "thrive" as username type in "thrive" as the password.

Note: if you have a small amount of ram you may want to limit the
setup script to only use a few CPU cores for building. To do this add
`-j 2` to the end of the setup command.

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

Windows note: when building Thrive in visual studio select
RelWithDebInfo configuration (instead of Debug). Otherwise the build
may fail.
