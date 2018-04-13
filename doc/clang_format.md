Using Clang Format
==================

To use clang format you must first install clang. On windows you
should use the clang installers and then edit your path so that the
installed folder with `clang-format.exe` is added to path. You can
optionally install visual studio integration for clang format.

On Linux use your package manager to install clang it will also
include clang-format. If not search your os's repository for the
package that has clang-format.

Once installed you can run `clang-format -i filename` on a thrive
source file, which should automatically find Thrive's clang format
options and format it correctly. But the recommended way is to install
a plugin for your editor to run it with a hotkey or automatically on
save.
