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

- Some common short names are accepted (and even preferred): i, k, a,
  b used in loops (x, y, z used in loops that deal with coordinates or
  math), e used in `catch` blocks as the exception name. Other
  variables in loops and elsewhere need to be named with actually
  descriptive variable names.
  
- Similarly, some very common abbreviations are used in the code,
  and can (and should) thus be used when naming variables. These are
  however *rare* exceptions, not the rule. The allowed abbreviations 
  are listed below. No other abbreviation should be used without prior
  discussion (and good reasons).
  - `min`
  - `max`
  - `pos`
  - `rot`
  - `rect` (when related to class names and variables holding instances of those classes)

- Variables and functions are camelCase or PascalCase depending on
  their visibilty. Classes are PascalCase with leading upper
  case. StyleCop enforces these rules. Constants are all upper case
  with `SNAKE_CASE` (underscores). Enums may be PascalCase or
  `ALL_UPPER_CASE` (but PascalCase is very strongly preferred).

- Code filenames are the same case as the primary class in them,
  ie. PascalCase. Also Godot scenes and other resources should be
  named in PascalCase and saved in files that match their name. Thrive
  is a cross-platform project and some platforms use case-sensitive
  file systems (Unix). Other files and folders that don't need to be
  named the same as the class in them are named with all lowercase
  with underscores separating the words.

- Use British English, unless something like Godot requires to use American spelling.

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
  comment. All XML comments must begin with a `summary` section to
  explain what something is, and after that what it is used for. If
  the usage explanation is long or there are extra information to
  include, put those into a paragraph inside a `remarks` section.

- In XML comments each nesting level is intended 2 spaces more than
  the previous level.

- In XML comments the following elements should be on a single line:
  param, returns, template param, exception. If the text is so long
  that it doesn't fit within a single line, then those previously
  mentioned elements should also be split on multiple lines. The
  following should always be on multiple lines: summary,
  remarks. Additionally remarks should contain individual `para`
  elements that have the actual text in them. For example:
  ```xml
  <remarks>
    <para>
      This is a remark.
    </para>
  </remarks>
  ```

- Inline comments inside functions can and should be used to describe
  why you wrote the function like this (and, sometimes more
  importantly, why you didn't go another way). They should **not** use
  XML style. You can use these types of comments to section of files
  or put labels in methods to mark what different parts are for.

- Don't place inline comments at the end of lines, place them on their
  own lines. Don't use comments after a piece of code on a single
  line.

- Start comments with a capital letter, unless it is a commented out
  code block or a keyword.

- The `returns` section of an XML can be omitted if it adds nothing
  valuable. For example a method like `public List<Organelle>
  GetOrganelles()` having documentation that it "returns a list of
  organelles" doesn't provide any useful information to the person
  reading the code that they couldn't see directly from the method
  signature.

- Empty lines are encouraged between blocks of code to improve
  readability. Blank space is your friend, not your enemy. Separate
  out logically different parts of a method with blank lines. For
  example, if you have variable assignments or declarations before an
  if or a loop that don't very strongly belong together, add a blank
  line.

- Use preincrement (`++i`) in loops and other cases, unless you
  actually need post increment.

- There should not be a line change in method declarations before the
  name of the method. Prefer adding a line break after the opening
  `(`

- Switch statements should use braces when there is more than one line
  of code followed by a break/return or a variable is defined. Switch
  statements should have both of their braces on a new line. When
  using braces `break` should be inside the closing brace and not
  after it.

- Multiline `if`s need to use braces or if there is an `else` or an
  `else if` clause then braces need to be used even if the actual
  bodies are just a single line. Just a single if (without else) with
  a single line body can be written without braces, and this style
  should be preferred.
  
- Ternary operators (`a ? b : c`) can be used instead of `if ... else`
  statements as long as they are kept readable. Nested ternaries are 
  always banned and should be systematically replaced by if-blocks.

- Single line variables (and properties) can be next to each other
  without a blank line. Other variables and class elements should have
  a blank line separating them.

- Don't declare multiple local variables on the same line, instead
  place each declaration on its own line

- Variables should by default use `var`. Exception are primitive types
  where specific control on the data types is preferred, for example
  conversions between floats and ints to ensure the calculation
  performs in an expected way.

- Variables should be private by default and only be made public if
  that is required. Properties should be used when some action is
  needed when a variable is changed, instead of creating setter or
  getter methods. Overall public properties should be preferred in
  classes over fields.

- Properties should be very strongly preferred over getter or setter
  methods. Only when a parameter is needed, is a getter method a good
  idea. Before making a property public get or set, think if it is
  really needed. To reduce code complexity unnecessary properties
  should not be accessible to outside code.

- When setting things that might require validation going through a
  property should be preferred, even in the same class to avoid
  mistakes in skipping some logic by directly assigning a field.

- Variables should be defined in the smallest possible scope. We
  aren't writing ancient C here.

- `uint` type should not be used without a very good reason.

- Unrelated uses should not share the same variable. Instead they
  should locally define their own variable instance.

- Avoid globals. Especially in object trees where you can easily
  enough pass the reference along.

- Prefer `List` and other concrete containers over `IList` and similar
  interfaces. `IList` should be used only in very special cases that
  require it.

- Methods should not use `=> style` bodies, properties when they are
  short should use that style bodies.

- Prefer early returns in methods to avoid unnecessary
  intendation. Check assumptions about the parameters of a method at
  the start and return early with an error if the inputs are not
  valid.

- Don't use LINQ unnecessarily, for example if there's a built in
  method. For list emptiness check `.Count < 1`

- Prefer to write out code rather than using very complex LINQ
  chains. Use complex LINQ sparingly. If LINQ would need many nested
  statements in lambdas, normal code should be used instead

- Prefer to use short form names in LINQ statements instead of one
  letter names, or at least use a sensible letter and don't always use
  x, instead use i for item, c for cells etc.

- Don't add a `Dispose` method to classes that don't need it.

- Prefer using declarations (C# 8 feature) over using statement
  blocks. If you need to reduce the scope of the using variables,
  start a new block with curly braces and use the using declaration
  inside it.

- Continuous Integration (CI) will check if the formatting scripts and
  tools find some problems in your code. You should fix these if your
  pull request (PR) fails the CI build.

- Ruby files should be named with snake_case. When intended as
  runnable scripts they need to begin with a shebang and be marked
  executable. RuboCop rules should be followed in ruby
  files. `snake_case` is used for variable and function names.

- You should familiarize yourself with the codebase at least somewhat
  so that you can use similar approaches in new code as is used in
  existing code as new code should follow the conventions (even if not
  mentioned in this guide) that existing code has established.

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

- Node names should not contain spaces, instead use PascalCase naming.

- For connecting signals, use `nameof` to refer to methods whenever possible
  to reduce the chance of mistakes when methods are renamed.

- If you need to keep track of child elements that are added through a
  single place, keep them in a List or Dictionary instead of asking
  Godot for the children and doing a bunch of extra casts.

- When destroying child Nodes or Controls take care to detach them
  first, in cases that having them hang around for one more frame
  causes issues, as that doesn't happen if you just call
  `QueueFree`. You can instead call `DetachAndQueueFree`
  instead to detach them from parents automatically.

- To support keeping references to game objects, we have `IEntity` interface
  that all game objects need to implement. To keep references to these
  entities, use the `EntityReference<T>` class. That class will
  automatically clear the reference when the entity is destroyed. To
  make this work all entity types need to properly implement the
  `OnDestroyed` method and all code destroying entities must call that
  method before freeing the Godot Node. Normal references can be used
  for a single frame, and in fact if a single `EntityReference` needs
  to be used multiple times, it is preferred to read out the value
  from it to a local variable first.

- The order of Godot overridden methods in a class should be in the
  following order: (class constructor), _Ready, _ExitTree, _Process,
  _Input, _UnhandledInput, (other callbacks)

- If you need to access parent objects, don't make a static public
  instance variables, instead pass callbacks etc. around to allow the
  child objects to notify the parent objects, this is to reduce
  coupling and global variables.

- To remove all children of a Node use `FreeChildren` or
  `QueueFreeChildren` extension methods.

- DO NOT DISPOSE Godot Node derived objects, call QueueFree or Free
  instead.

- When using `GD.PrintErr` don't use string concatenation, use the
  multi argument form instead, for example: `GD.PrintErr("My value is:
  ", variable);`

- Don't use text in the GUI with leading or trailing spaces to add
  padding, see previous bullet instead.

- You should follow general GUI standards in designing UI. Use widgets
  that are meant for whatever kind of interaction you are designing.

- When adding window dialogs to the game, consider using the
  `CustomDialog` type rather than the built-in `WindowDialog` to ensure
  consistency across the GUI. This is because the custom implementation
  offer a much more customized styling and additional functionality.

- Question popups should have a short title ending in a question mark
  (`?`). The content of the popup should give more details and also
  end with a question.

- Popups should be shown with `PopupCenteredShrink()`. If size
  shrinking is not desired, `PopupCentered()` should be used
  instead. Unless there's a good reason why something else is
  required, prefer to use either of them. Don't use `Popup_` prefer to
  use `Show` or `ShowModal` only if those both don't work then you can
  consider using `Popup_`.

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

- All images used in the GUI should have mipmaps on in the import
  options.

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

- Registry items (for example organelles) should use camelCase for their
  internal names (IRegistryType.InternalName), and not snake_case.
  Otherwise other names that follow the internal names will violate other
  naming conventions.

- Do not use `<br>` in markdown unless it is a table where line breaks
  need to be tightly controlled. Use blank lines instead of
  `<br>`. Also don't use `<hr>` use `---` instead.

- For translations see the specific instructions in
  [working_with_translations.md](working_with_translations.md)

- For PRs don't run / include locale `.po` file changes if there
  are only changes to the reference line numbers. This is done to
  reduce the amount of changes PRs contain, but also means that the
  reference line numbers are sometimes slightly out of date.

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

- An exception to the above rule are the automatic PRs from weblate,
  those must be merged normally, otherwise weblate won't detect that
  correctly and fixing that requires a lot of manual merging. So merge
  weblate PRs as separate commits, not squashed.

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
