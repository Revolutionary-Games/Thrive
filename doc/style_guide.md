Code Style Guide
================

To maintain a consistent coding style, contributors should follow the
rules outlined on this page. This style guide is separated into three
parts: code rules, other file rules, and guidelines for using git.

The style rules are intended to increase readability of the source
code. The most important rule of all is: **Use common sense**. If you
have to break some rules to make the code more readable (and not just
for you, but for everyone who has to read your code), break
it. Breaking stylecop enforced rules should only be done in special
cases.

Note: we are new to using StyleCop so the list of rules and options
for it can be debated and changed in the future.

Code style rules
----------------

- Indentation is 4 spaces

- Names (that includes variables, functions and classes) should be
  descriptive.  Avoid abbreviations. Do not shorten variable names
  just to save key strokes, it will be read far more often than it
  will be written.

- Variables and functions are camelCase or PascalCase depending on
  their visibilty. Classes are PascalCase with leading upper
  case. StyleCop enforces these rules. Constants are all upper case
  with SNAKE_CASE (underscores).

- Code filenames are the same case as the primary class in them,
  ie. PascalCase. Also Godot scenes and other resources should be
  named in PascalCase and saved in files that match their name. Thrive
  is a cross-platform project and some platforms use case-sensitive
  file systems (Unix).  Other files and folders that don't need to be
  named the same as the class in them are named with all lowercase
  with underscores separating the words.

- C# file lines should have a maximum width of 120 columns.

- C# code should by default not be in a namespace. When code is in a
  namespace all using statements should be within the namespace.

- Build your code with warnings enabled to see things StyleCop
  complains about. If you don't automatic checks on your code will
  fail.
  
- Due to StyleCop not having rules for everything, there are
  additional rules implemented by `check_formatting.rb` which you
  should run before committing to make sure there are no issues in
  your code.

- All classes and their public and protected members should be documented by
  xml comments in the header file. If the function's purpose is clear
  from the name and its parameters documentation can be omitted.

- Inline comments inside functions can and should be used to describe why
  you wrote the function like this (and, sometimes more importantly, why you
  didn't go another way).

- Empty lines are encouraged between blocks of code to improve readability.

- Variables should be private by default and only be made public if
  that is required. Properties should be used when some action is
  needed when a variable is changed, instead of creating setter or
  getter methods.

Other files
-----------

- At this time GDScript is not allowed. No .gd files should exist in
  the Thrive repository.
  
- Simulation configuration json files should be named using snake_case.

- JSON files should be sensibly intended.

Git
---

- Do not work in the master branch, always create a private feature
  branch if you want to edit something. Even when working with a fork
  this is recommended to reduce problems with subsequent pull
  requests.

- If you don't have access to the main repository yet (you will be
  granted access after your first accepted pull request) fork the
  Thrive repository and work in your fork and once done open a pull
  request.

- If you are working on a GitHub issue, your feature branch's name should
  begin with the issue number, followed by an underscore, followed by a
  short, descriptive name in lower_case_with_underscores. The name should
  be short, but descriptive enough that you know what the feature branch is
  about without looking up the GitHub ticket.

- Commit early and frequently, even if the code doesn't run or even
  compile. It is recommended to use an interactive program for staging
  parts of files. So even if you have unrelated changes within the
  same file, you can still separate them. And you don't accidentally
  commit something you didn't intent to.

- When the master branch is updated, you should usually keep your
  feature branch as-is. If you really need the new features from
  master, do a merge. Or if there is a merge conflict preventing your
  pull request from being merged. Quite often the master branch needs
  to be merged in before merging a pull request. For this it is
  cleaner if the pull request is rebased onto master, but this is not
  required.

- When a feature branch is done, open a pull request on GitHub so that
  others can review it. Chances are that during this review, there
  will still be issues to be resolved before the branch can be merged
  into master. You can make a draft pull request if you want feedback
  but want to clearly state that your changes are not ready yet.

- To keep master's commit history somewhat clean, your commits in the
  feature branch will be "squashed" into a single commit. This can be
  done (take note people accepting pull requests) from the Github
  accept pull request button, hit the arrow to the right side and
  select "merge and squash". Bigger feature branches that are dozens
  of commits or from multiple people need to be manually squashed into
  a few commits while keeping commits from different authors separate
  in order to attribute all of the authors on Github feeds. These kind
  of pull requests can be merged normally **if** all the commits in them
  are "clean" to not dirty up master.

- For maintainers: When manually merging (which is something you
  should avoid) GitHub requires a merge commit to recognize the
  merging of a pull request. A "git merge --squash" does not create a
  merge commit and will leave the pull request's branch "dangling". To
  make GitHub properly reflect the merge, follow the procedure
  outlined in the previous bullet point, then click the "Merge Pull
  Request" button on GitHub (or do a normal "git merge").
