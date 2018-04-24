Code Style Guide
================

To maintain a consistent coding style, contributors should follow the rules 
outlined on this page. This style guide is separated into four parts: common 
rules, rules specific for C++, rules specific for AngelScript and guidelines for
using git.

The style rules are intended to increase readability of the source code. The 
most important rule of all is: **Use common sense**. If you have to break 
some rules to make the code more readable (and not just for you, but for 
everyone who has to read your code), break it.

Common (Both C++ and AngelScript)
--------------------------------------

- Indentation is 4 spaces

- Names (that includes variables, functions and classes) should be descriptive.
  Avoid abbreviations. Do not shorten variable names just to save key strokes, 
  it will be read far more often than it will be written.

- Variables and functions are camelCase with leading lower case. Classes are 
  CamelCase with leading upper case. Constants are CONSTANT_CASE with 
  underscores.

- Filenames are lower_case with underscores. The reason for this is
  that Thrive is a cross-platform project and some platforms use
  case-sensitive file systems (Unix) while others use case-insensitive
  file systems (Windows). Exceptions are the CMakeLists.txt files and
  a few of the other core files, which need to be named like this for
  them to work.

C++
---

- Macros are CONSTANT_CASE

- Header files end in .h, source files in .cpp

- Header files should begin with `#pragma once`. Old-style header 
  guards (with `ifdef`) are discouraged because they are very verbose and
  the pragma is understood by all relevant compilers.
  
- Format your code with [clang-format](clang_format.md)

- Opening braces go in the same line as the control statement, closing braces
  are aligned below the first character of the opening control statement

- Keep header files minimal. Ideally, they show only functions / classes that
  are useful to other code. All internal helper functions / classes should be
  declared and defined in the source file.

- All classes and their public and protected members should be documented by
  doxygen comments in the header file. If the function's purpose is clear 
  from the name and its parameters (which should be the usual case), the 
  comment can be very basic and only serves to shut up doxygen warnings about
  undocumented stuff.

- Inline comments inside functions can and should be used to describe why
  you wrote the function like this (and, sometimes more importantly, why you
  didn't go another way).

- Member variables of classes are prefixed with \p m_. This is to 
  differentiate them from local or global variables when using their 
  unqualified name (without `this->`) inside member functions. The prefix can
  be omitted for very simple structs if they don't have member functions and
  serve only as data container.

(- When calling member functions from another member function, their names are
  qualified with `this->` to differentiate them from global non-member 
  functions.)

- Function signatures are formatted like the clang-format options file makes them to be formatted

- For non-trivial classes that would pull in a lot of other headers, use the pimpl idiom to hide implem  entation details and only include the ton of headers in the .cpp file.

  ```cpp
    // In the header:
    #include <memory> // Include for std::unique_ptr

    class MyClass {
        // ...

    public:

        // Constructor required, doesn't need to be
        // default constructor, though
        MyClass();

        // Destructor required.
        ~MyClass();

    private:
      
        struct Implementation;
        std::unique_ptr<Implementation> m_impl;
    };
  ```
  
  ```cpp
    // In the source file:

    struct MyClass::Implementation {
        // Private stuff here
    };

    MyClass::MyClass()
      : m_impl(new Implementation()) // Initialize pimpl
    {
    }

    MyClass::~MyClass() {} // Define destructor
  ```
  
- Try to avoid include statements inside header files unless
  absolutely necessary. Prefer forward declarations and put the
  include inside the source file instead. And use the pimpl idiom if
  this cannot be avoided and the headers are large.

- Prefer C++11's `using` over `typedef`. With the `using` keyword, type 
  aliases look more like familiar variable assignment, with no ambiguity as
  to which is the newly defined type name.

- Virtual member functions overridden in derived classes are marked with the 
  C++11 `override` keyword. This will (correctly) cause a compile time error 
  when the function signature in the base class changes and the programmer 
  forgot to update the derived class.

- Classes not intended as base classes are marked with the `final` keyword
  like this:
  
  ```cpp
    class MyClass final {
        // ...
    };
  ```

- Header includes should be split by project with an empty line
  between. The order is first Thrive headers then Leviathan headers
  then any other library and finally standard headers. All of these
  blocks are sorted alphabetically. Example:

  ```cpp
      #include "engine/component_types.h"
      #include "engine/typedefs.h"

      #include <Entities/Component.h>
      #include <Entities/System.h>
      
      #include <OgreMatrix4.h>

      #include <vector>
      #include <unordered_map>
  ```

- Functions and data members inside classes should be split and ordered logically.


AngelScript
-----------

- A class's public data members are *not* prefixed by `m_`, unlike C++. 

(This is because in AngelScript, all member variables are accessed with their qualified
  names (like `this.memberVariable`), so there is no need to mark them.)

- A class's private data members and functions are declared `private`
  (everything is public by default) and optionally prefixed with an
  underscore. This is a convention adopted from Python's PEP8 style
  guide.

- Doxygen does not support AngelScript natively, but for consistency's sake, AngelScript 
  classes and functions are still documented with doxygen style comments.

- For consistency with C++ adding semicolons after class declarations
  is recommended, but not an error if omitted. This is one of the
  biggest syntax differences of AngelScript vs C++ besides the handle
  types.

Git
---

- Do not work in the master branch, always create a private feature branch
  if you want to edit something
  
- If you don't have access to the main repository yet (you will be
  granted access after your first accepted pull request) fork the
  Thrive repository and work in your fork and once done open a pull
  request.

- If you are working on a GitHub issue, your feature branch's name should
  begin with the issue number, followed by an underscore, followed by a
  short, descriptive name in lower_case_with_underscores. The name should
  be short, but descriptive enough that you know what the feature branch is
  about without looking up the GitHub ticket.

- Commit early and frequently, even if the code doesn't run or even compile.
  I recommend git-cola (available for all major platforms) as a tool for
  composing good commits. It lets you stage files linewise in a convenient
  interface. So even if you have unrelated changes within the same file,
  you can still separate them.

- When the master branch is updated, you should usually keep your
  feature branch as-is. If you really need the new features from
  master, do a merge. Or if there is a merge conflict preventing your
  pull request from being merged.

- When a feature branch is done, open a pull request on GitHub so that others
  can review it. Chances are that during this review, there will still be
  issues to be resolved before the branch can be merged into master.

- To keep master's commit history somewhat clean, your commits in the
  feature branch will be "squashed" into a single commit. This can be
  done (take note people accepting pull requests) from the Github
  accept pull request button, hit the arrow to the right side and
  select "merge and squash". Bigger feature branches that are dozens
  of commits from multiple people will be merged normally to give
  credit to all of the authors on Github feeds.

- For maintainers: When manually squashing GitHub (which is something
  you should avoid) requires a merge commit to recognize the merging
  of a pull request. A "git merge --squash" does not create a merge
  commit and will leave the pull request's branch "dangling". To make
  GitHub properly reflect the merge, follow the procedure outlined in
  the previous bullet point, then click the "Merge Pull Request"
  button on GitHub (or do a normal "git merge".
