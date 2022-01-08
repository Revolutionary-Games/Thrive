Profiling
=========

There's two ways to profile the game to figure out what is causing
a slowdown: mono and native.

Mono Profiling
==============

You can use a mono profiler to run the game through (at least Rider
has an option for this with all the required plugins and components
installed). You can then look at the call graph to see where time is
spent.

Native Profiling
================

For this you need to compile a custom version of Godot with gprof
enabled (valgrind seems to crash the game meaning it is not
usable). Gperftools might be a viable alternative, but it has not been
tested yet and it requires extra libraries that are compiled first.

For gprof you first need to compile a custom Godot editor with (NOTE:
you need to do a mono build of Godot, for how to do that please see
Godot documentation regarding the glue generation):
```sh
scons -j16 production=yes verbose=yes warnings=no progress=no \
  debug_symbols=yes separate_debug_symbols=no use_lto=yes \
  use_static_cpp=yes platform=x11 module_mono_enabled=yes \
  mono_static=yes mono_prefix=/path/to/mono tools=yes \
  target=release_debug copy_mono_root=yes \
  CCFLAGS=-pg CFLAGS=-pg CXXFLAGS=-pg LINKFLAGS=-pg
```

Then run the game with the made executable:
```sh
cd Thrive
~/path/to/godot.x11.opt.tools.64.mono
```

You should run the game for long enough for enough profiling data to
be generated. A couple of minutes while experiencing the slow
performance should be fine.

This generates a gmon.out, which you can process and make an image if
you have `gprof2dot` and `dot` tools:
```sh
gprof /path/to/godot.x11.opt.tools.64.mono gmon.out > out.txt
gprof2dot out.txt | dot -Tpng -o output.png
```

Now you can view the out.txt file and the output.png image to see the
results.

