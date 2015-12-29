# Copyright (c) 2010 Xynilex Project
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,  
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

macro(GetLibraryPathHints name)
  set(${name}_PATH_HINTS
    "${MINGW_ENV}/${name}"
    "~/${name}"
    "/usr"
    "/usr/lib"
    "/usr/include"
    "/usr/local"
    "/usr/local/lib"
    "/usr/local/include"
    "${CMAKE_CURRENT_SOURCE_DIR}/Libs"
    "${CMAKE_CURRENT_SOURCE_DIR}/Libs/${name}"
  )

  set(${name}_PATH_SUFFIX_HINTS
    include
    Include
    includes
    Includes
    lib
    Lib
    libs
    Libs
    bin
    Bin
  )
endmacro()
