Code Style Guide
================

To maintain a consistent coding style, contributors should follow the
rules outlined on this page. This style guide is separated into three
parts: code rules, other file rules, and guidelines for using git.

The style rules are intended to increase readability of the source
code. The most important rule of all is: **Use common sense** and
follow what the automatic formatting tools want. If you have to break
some rules to make the code more readable (and not just for you, but
for everyone who has to read your code), break it. Breaking stylecop
enforced rules, with an ignore directive, should only be done in
special cases. If some rule is especially troublesome it can be
discussed whether it can be disabled entirely.

Code style rules
----------------

- Indentation is 4 spaces. Continued statements are indented one level
  higher.

- Names (that includes variables, functions and classes) should be
  descriptive. Avoid abbreviations. Do not shorten variable names just
  to save key strokes, it will be read far more often than it will be
  written. Single character variable names can be used in for
  loops. They should be avoided everywhere else.

- Variables and functions are camelCase or PascalCase depending on
  their visibilty. Classes are PascalCase with leading upper
  case. StyleCop enforces these rules. Constants are all upper case
  with SNAKE_CASE (underscores). Enums may be PascalCase or
  ALL_UPPER_CASE.

- Code filenames are the same case as the primary class in them,
  ie. PascalCase. Also Godot scenes and other resources should be
  named in PascalCase and saved in files that match their name. Thrive
  is a cross-platform project and some platforms use case-sensitive
  file systems (Unix). Other files and folders that don't need to be
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

- For faster rebuilding have a look at the scripts in
  scripts/fast_build. With the `toggle_analysis_mode.rb` script it is
  possible to turn off the analysis so that small tweaks to the game
  are faster to test. Next time you run the formatting script the
  checks should get turned back on.

- All classes and their public and protected members should be
  documented by XML comments. If the function's purpose is clear from
  the name, then its documentation can be omitted. If there is a
  comment on a single construct (class, method etc.) it must be an XML
  comment.

- Inline comments inside functions can and should be used to describe
  why you wrote the function like this (and, sometimes more
  importantly, why you didn't go another way). They should **not** use
  XML style. You can use these types of comments to section of files
  or put labels in methods to mark what different parts are for.

- Empty lines are encouraged between blocks of code to improve
  readability. Blank space is your friend, not your enemy. Separate
  out logically different parts of a method with blank lines.

- Switch statements should use braces when there is more than one line
  of code followed by a break/return or a variable is defined. Switch
  statements should have both of their braces on a new line. When
  using braces `break` should be inside the closing brace and not
  after it.

- Single line variables can be next to each other without a blank
  line. Other variables and class elements should have a blank line
  separating them.

- Variables should by default use `var`. Exception are primitive types
  where specific control on the data types is preferred, for example
  conversions between floats and ints to ensure the calculation
  performs in an expected way.

- Variables should be private by default and only be made public if
  that is required. Properties should be used when some action is
  needed when a variable is changed, instead of creating setter or
  getter methods. Overall public properties should be preferred in
  classes over fields.

- Variables should be defined in the smallest possible scope. We
  aren't writing ancient C here.

- `uint` type should not be used without a very good reason.

- Unrelated uses should not share the same variable. Instead they
  should locally define their own variable instance.

- Methods should not use `=> style` bodies, properties when they are
  short should use that style bodies.

- Continuous Integration (CI) will check if the formatting scripts and
  tools find some problems in your code. You should fix these if your
  pull request (PR) fails the CI build.

- Ruby files should be named with snake_case. When intended as
  runnable scripts they need to begin with a shebang and be marked
  executable.

- Defensive programming is recommended. The idea is to write code that
  expects other parts of the codebase to mess up somewhere. For example,
  when checking if a species is extinct, instead of checking
  `population == 0`, it is recommended to do `population <= 0` to guard
  against negative population bugs.

- Finally you should attempt to reach the abstract goal of clean
  code. Here are some concepts that are indicative of good code (and
  breaking these can be bad code): Liskov substitution principle,
  single purpose princible, logically putting same kind of code in the
  same place, avoid repetition, avoid expensive operations in a loop,
  prefer simpler code to understand. Avoid anti-patterns, for example
  God class.

Godot usage
-----------

- Do not use Control margins to try to position elements, that's not good
  Godot usage. Use proper parent container and min size instead.

- For spacing elements use either a spacer (that has a visual
  appearance) or for invisible space use an empty Control with rect
  minsize set to the amount of blank you want.

- If you need to keep track of child elements that are added through a
  single place, keep them in a List or Dictionary instead of asking
  Godot for the children and doing a bunch of extra casts.

- Don't use text in the GUI with leading or trailing spaces to add
  padding, see previous bullet instead.

- You should follow general GUI standards in designing UI. Use widgets
  that are meant for whatever kind of interaction you are designing.

- Question popups should have a short title ending in a question mark
  (`?`). The content of the popup should give more details and also
  end with a question.

- Popups should be shown with `PopupCenteredMinsize()` unless there's
  a good reason why something else is required.

- Don't use `Godot.Color(string)` constructor, unless explicitly
  needed. An explicit need is for example loading from JSON or from
  user input. String literal colours should not be used in C# source
  code.

- When using fonts, don't directly load the .ttf file with an embedded
  font in a scene file. Instead create a font definition in
  `src/gui_common/fonts` folder and use that. This is needed because
  all fonts must have fallback fonts defined for translations that use
  character sets that aren't in the main fonts, for example
  Chinese. All fonts should be truetype (`.ttf`) and stored in
  `assets/fonts`.

Other files
-----------

- At this time GDScript is not allowed. No `.gd` files should exist in
  the Thrive repository.

- Simulation configuration JSON files should be named using snake_case.

- JSON files need to be formatted with jsonlint. There is a CI check
  that will complain if they aren't. This is a part of the formatting
  check script.

- New JSON files should prefer PascalCase keys. Existing JSON files
  should stick to what other parts of that file use.

Git
---

- Do not work in the master branch, always create a private feature
  branch if you want to edit something. Even when working with a fork
  this is recommended to reduce problems with subsequent pull
  requests (PR).

- If you don't have access to the main repository yet (you will be
  granted access after your first accepted pull request and you have
  joined the team) fork the Thrive repository and work in your fork
  and once done open a pull request.

- If you haven't made many pull requests in the past, it is highly
  recommended to keep anything not directly needed in your PR away
  from it to make reviewing easier. Adding refactoring (other than
  refactoring that is asked for by an open issue) or style changes in
  your first PR will make it more of a hassle to get it accepted.

- If you are working on a GitHub issue, your feature branch's name
  should begin with the issue number, followed by an underscore,
  followed by a short, descriptive name in
  lower_case_with_underscores. The name should be short, but
  descriptive enough that you know what the feature branch is about
  without looking up the GitHub ticket. The second best is to have a
  descriptive name for a branch that uses underscores to separate
  words.

- Commit early and frequently, even if the code doesn't run or even
  compile. It is recommended to use an interactive program for staging
  parts of files. So even if you have unrelated changes within the
  same file, you can still separate them. And you don't accidentally
  commit something you didn't intent to.

- When the master branch is updated, you should usually keep your
  feature branch as-is. If you really need the new features from
  master, do a merge. Or if there is a merge conflict preventing your
  pull request from being merged. Quite often the master branch needs
  to be merged in before merging a pull request, this can be done
  directly on Github if there aren't any conflicts. If you want you
  can alternatively rebase the feature branch onto master, but this is
  not required normally. In a case where a direct merge (not squash)
  is wanted to master then a rebase needs to be performed.

- When a feature branch is done, open a pull request on GitHub so that
  others can review it. Chances are that during this review, there
  will still be issues to be resolved before the branch can be merged
  into master. You can make a draft pull request if you want feedback
  but want to clearly state that your changes are not ready yet.

- Pull requests should try to be focused on a single thing so that
  reviewing them is easier. General refactoring should not be combined
  in the same PR as a new feature as overall refactoring causes a ton
  of changes that needs to be reviewed careful and combining that with
  a new feature causes even more work.

- To keep master's commit history somewhat clean, your commits in the
  feature branch will be "squashed" into a single commit. This can be
  done (take note people accepting pull requests) from the Github
  accept pull request button, hit the arrow to the right side and
  select "merge and squash". Bigger feature branches that are dozens
  of commits or from multiple people need to be manually squashed into
  a few commits while keeping commits from different authors separate
  in order to attribute all of the authors on Github feeds. These kind
  of pull requests can be merged normally **if** all the commits in
  them are "clean" to not dirty up master. In cases where co-authored
  by attribution is enough, these pull requests can be squashed
  normally from Github. This doesn't give as much attribution to
  everyone who contributed to the pull request, but can be suitable in
  some cases where someone does a small fix to get things ready for a
  merge.

- You should not leave the co-authored-by line in a squashed commit if
  all you did was merge master into the branch to make the merge show
  up as green on Github.

- For maintainers: When manually merging (which is something you
  should avoid) GitHub requires a merge commit to recognize the
  merging of a pull request. A "git merge --squash" does not create a
  merge commit and will leave the pull request's branch "dangling". To
  make GitHub properly reflect the merge, follow the procedure
  outlined in the previous bullet point, then click the "Merge Pull
  Request" button on GitHub (or do a normal "git merge").

- All team members are encouraged to review pull requests. However,
  you shouldn't go and merge PRs if you haven't discussed it with the
  programming team lead yet / it isn't approved by them yet.
