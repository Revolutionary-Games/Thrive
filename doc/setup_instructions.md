What's this?
============

These are the setup instructions for compiling Thrive.

Important Note: If you run into any trouble with the setup scripts, please 
bring them up [on the forums](https://community.revolutionarygamesstudio.com/c/dev-help), 
or if you are a team member you can ask on the development discord or open a github issue. 

This tutorial is also in (a slightly outdated) video form for: [Windows](https://www.youtube.com/watch?v=eiQsxKCpOhY) 
    and [Linux (Fedora and Ubuntu)](https://www.youtube.com/watch?v=_ZWBTlIo9W4)

If you are a team member you can ask for help on the forums:
    [Private Developer Forums](http://forum.revolutionarygamesstudio.com/c/programming)

You can also join and ask on our [community discord](https://discordapp.com/invite/FZxDQ4H)

Thank you!

Prerequisites
=============

10 GiB of disk space and about 60 minutes of compile time on a good CPU.

Follow the Leviathan Engine Prequisites installation instructions
[here](https://leviathanengine.com/doc/develop/Documentation/html/dc/d9e/prerequisites.html).

Important: you should read the whole document before starting, as many
common pitfalls and issues have fixes given later in the document. Also, DO
NOT SKIP ANY STEPS, otherwise you will initially fail and have to clear
the caches, which is the easiest to do by just deleting the entire
folder and starting again. When you are done with the prerequisites
page, return here instead of continuing to the building Leviathan page,
which is irrelevant to Thrive.

Linters
-------

Thrive uses automatic formatting of source code to keep style as
consistent as possible. For this reason you need to install some
additional tools:

- [Clang](http://releases.llvm.org/download.html). You need at least
  version 6.0.0. On Linux you can use your OS's package manager if it
  has a new enough version.
- [nodejs](https://nodejs.org/en/download/). If you are on Linux you
  should use your OS's package manager to install nodejs.
- eslint (with eslint-plugin-html) and stylelint. Install with npm
  once you have nodejs installed. On both Windows and Linux,
  in the thrive base folder: `npm install eslint stylelint
  eslint-plugin-html`.

When you are getting ready to commit you should run `ruby
RunCodeFormatting.rb` in order to automatically run all of the
formatting tools. Make sure that that script doesn't report any errors
before committing.

Additionally, for local testing, you might want to install `http-server`
via `npm install -g http-server`. If you're on Linux you will probably
need `sudo`. You can then run a test web server with `http-server -c-1`
in the thrive base folder.


Building Thrive
===============

Clone
-----

Open a terminal on linux or a command prompt (or powershell) on windows to a folder where you want to place the Thrive folder.

Note: a path with spaces in it WILL NOT WORK, so to avoid issues you
should choose a folder like `~/projects` or `C:/projects`. Also, long
paths don't work on Windows as the setup needs the path in which it is
ran to be less than 90 characters, so run git clone in
`C:\projects` so that you end up the thrive folder being
`C:\projects\Thrive`. One additional potential problem is non-English
characters in the path name, for example if your path includes
"työpöytä", the setup will fail.

Windows tip: shift right-click in a folder and select "Open command
prompt here" or "Open powershell here" to open a cmd window to the
folder.

And now run

```
git clone https://github.com/Revolutionary-Games/Thrive.git
cd Thrive
```

To get the Thrive repository cloned.

For devs working on new features: switch to a feature branch or create
one. For example `git checkout fix_everything`. This keeps the main
branch clean as other branches can be merged through pull requests on
github which is the recommended way to get your code into Thrive.

If you aren't on the team (you don't have push access on github)
create a fork on github and use the url of that instead of the one above.


Running Setup
-------------

Now you can run the setup script (make sure you have finished
with the prerequisites first) with:

```
ruby SetupThrive.rb
```

Note: if you have a small amount of ram you may want to limit the
setup script to only use a few CPU cores for building, or if you
don't want to dedicate all of your CPU. To do this add `-j 2` to
the end of the setup command with the number being the number of
cores you want to use, the default is to use all. Note: this may
not work for the dependencies (if so, that needs fixing)

### Unsupported Linuxes
If you are trying to compile on an unsupported Linux OS you should run
the setup like this:

```
ruby SetupThrive.rb --no-packagemanager --pretend-linux fedora
```

Note: you will have to manually install all the required packages.

Done
----

If the setup script succeeds, then congratulations, you have just
successfully built Thrive and can start working.

If it didn't work you can try these:

- [Leviathan Troubleshoothing tips](https://leviathanengine.com/doc/develop/Documentation/html/dc/dca/compiling_leviathan.html#compile_troubleshooting)
- Thrive community forums
- Thrive developer forums
- Thrive community discord
- Thrive developer discord (you can only access this if you are a team member)

to get help. Links to the forums and community discord
are at the top of this document.

Here are some quick tips for working on thrive:

- [Scripting
  documentation](https://leviathanengine.com/doc/develop/Documentation/html/d0/db5/angelscript_main.html)
- On Windows: when building Thrive in visual studio select
  `RelWithDebInfo` configuration (instead of Debug). Otherwise the build
  may fail. And make sure "Thrive" is the startup project to run it in the debugger.
- Whenever you change the assets or scripts you **must run cmake**, or your changes will not take effect.
  See [this](https://leviathanengine.com/doc/develop/Documentation/html/df/d4e/tutorial1.html#tutorial1recompiling)
  for more info.
- On Linux, you can use `cmake .. && make && (cd bin && ./Thrive)` in the build folder
  to compile and run.

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
