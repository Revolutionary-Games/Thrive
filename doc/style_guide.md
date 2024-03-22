Code Style Guide
================

To maintain a consistent coding style, contributors should follow the
rules outlined on this page. This style guide is separated into three
parts: code rules, other file rules, and guidelines for using git.

The style rules are intended to increase readability of the source
code for *humans* that will read the written code. The most important
rule of all is: **Use common sense** and follow what the automatic
formatting tools want. If you have to break some rules to make the
code more readable (and not just for you, but for everyone who has to
read your code), break it. Breaking stylecop enforced rules, with an
ignore directive, should only be done in special cases. If some rule
is especially troublesome it can be discussed whether it can be
disabled entirely.

Code style rules
----------------

- Indentation is 4 spaces. Continued statements are indented one level
  higher.

- Names (that includes variables, functions and classes) should be
  descriptive. Avoid abbreviations. Do not shorten variable names just
  to save key strokes, it will be read far more often than it will be
  written. Single character variable names can be used in for
  loops. They should be avoided everywhere else. LINQ is an exception
  to this rule (see below for more info)

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
  - `str`
  - `rect` (when related to class names and variables holding instances of those classes)
  - `tech` (short for technology)

- Variables and functions are camelCase or PascalCase depending on
  their visibility. Classes are PascalCase with leading upper
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

- Use British English, unless something like Godot requires you to use
  American spelling, for code comments, variables and in-game text. An
  exception to this is "meter" and other words that would end in
  "tre", spell those as "ter".

- C# file lines should have a maximum width of 120 columns.

- Comments should use the C++ style `//` or XML doc (when documenting
  language constructs like classes and properties) `///`. C-style
  comments `/*` should only be used when commenting out multiple lines
  of code that is important to keep for future reference or similar
  code that is only a part of a line. This comment style can also be
  kept in copyright notice sections. Elsewhere C-style comments should
  not be used.

- C# code should by default not be in a namespace. When code is in a
  namespace all using statements should be within the namespace.

- Build your code with warnings enabled to see things StyleCop
  complains about. If you don't automatic checks on your code will
  fail.

- Due to StyleCop not having rules for everything, there are
  additional rules implemented by a custom script (`dotnet run
  --project Scripts check`) which you should run before committing to
  make sure there are no issues in your code. This script can be
  enabled to run automatically with pre-commit.

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
  line. The exception to this rule is when using pragma warning suppressions
  then add the reason for the suppression at the end of the line. For
  JetBrains checks, place a separate comment before the suppression
  explaining the reason for the suppression.

- Start comments with a capital letter, unless it is a commented out
  code block or a keyword.

- Comments may end in a period. However it is only really recommended
  for comments that are multiple sentences long. For other comments
  it's suggested to not end them with a period.

- If there is any debate what a method / parameter means in pull
  request comments, it *must* be documented. The discussion is proof
  that it is not clear enough without a comment. Or a more descriptive
  name may also help in this situation.

- If you add a comment describing what an `if` checked (what the
  result is), put that comment inside the `if`. Comments describing
  what the `if` is checking should be put on a line before the if. For
  example: "Make sure the player is alive" should be placed before the
  `if` but a comment like "Player is alive" should be inside the
  braces of the `if` statement.

- Prefer to not specify the value for variables or constants explicitly in the
  comments as they can easily get outdated when somebody changes the actual
  values. If it has to be specified, care **must** be taken to always update
  that comment whenever the value is changed.

- The `returns` section of an XML can be omitted if it adds nothing
  valuable. For example a method like `public List<Organelle>
  GetOrganelles()` having documentation that it "returns a list of
  organelles" doesn't provide any useful information to the person
  reading the code that they couldn't see directly from the method
  signature.

- For the main Thrive.csproj 
  [nullable reference types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) 
  feature is turned on. All nullable reference warnings need to be 
  fixed. In registry types and Node derived types we consider
  the `_Ready` method and required JSON properties to be "constructor"
  initialized, meaning we suppress those nullability warnings (when
  the fields are never checked against null, see Godot usage section
  for an example). Here's an example of the null suppression:
  ```c#
  [Export]
  public NodePath ConflictDialogPath = null!;
  
  private CustomConfirmationDialog conflictDialog = null!;
  
  public override void _Ready()
  {
      conflictDialog = GetNode<CustomConfirmationDialog>(ConflictDialogPath);
  }  
  ```

- Private and other methods that are called in controlled manner
  may use nullability suppression for things that are checked
  always higher up the callstack or in a Node `_Ready` method.
  Other places are usually better to throw an `InvalidOperationException`
  when they are tried to be used on a non-initialized instance.

- For third party code, nullability should be disabled / enabled based
  on whether the code uses nullable reference types or not.

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

- For method parameters, don't use the "chop" style for dividing them
  on multiple lines, use the wrap style instead (ie. fill as many
  parameters on each line as fits within the width limit, instead of
  just one per line).

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

- Targeted new and `default` should be used without specifying type
  when the type is evident from the context. When the type is not
  clear the type needs to be specified.

- Variables should be defined in the smallest possible scope. We
  aren't writing ancient C here.

- `uint` type should not be used without a very good reason.

- `System.Random` should be avoided as its state cannot be
  saved. Instead use the random number types in the `Xoshiro`
  namespace (check the documentation on the types for which generators
  are suitable for what kinds of numeric types). General advice is to
  use 128-bit generators for 32-bit types (`int`, `float`) and 256-bit
  generators for 64-bit types (`long`, `double`).

- Unrelated uses should not share the same variable. Instead they
  should locally define their own variable instance.

- Avoid globals. Especially in object trees where you can easily
  enough pass the reference along.

- Do not use `string.Format` with a translated format string, as
  translation mistakes can crash the game in that case. Instead either
  use `LocalizedString`, `LocalizedStringBuilder`, or
  `StringUtils.FormatSafe`. Those ways will automatically catch
  exceptions from broken translations and return the format string
  un-formatted. `StringUtils` will likely want to be invoked as an
  extension method on the string (`"example".FormatSafe(...)`). If the
  format string is not user supplied, normal `string.Format` is allowed,
  but should be passed `CultureInfo.CurrentCulture` as the first
  parameter as we want text shown to the user in the user's selected
  locale.

- Prefer `List` and other concrete containers over `IList` and similar
  interfaces. `IList` should be used only in very special cases that
  require it. In many cases `IEnumerable` is the preferred type to use
  to not place constraints on other code unnecessarily.

- Methods should not use `=> style` bodies, properties when they are
  short should use that style bodies.

- Prefer early returns in methods to avoid unnecessary
  indentation. Check assumptions about the parameters of a method at
  the start and return early with an error if the inputs are not
  valid.

- Don't use LINQ unnecessarily, for example if there's a built in
  method. For list emptiness check `.Count < 1`

- Prefer to write out code rather than using very complex LINQ
  chains. Use complex LINQ sparingly. If LINQ would need many nested
  statements in lambdas, normal code should be used instead

- Prefer to use single letter variable names in LINQ statements, when
  they are clear enough. Don't always use x or another generic
  variable for these. Instead use the first letter that a more
  descriptive name would have. For example use "i" for "item", "c" for
  "cells" etc.

- If the word "percentage" is used in a variable name, the valid range
  of value *must be* 0-100. If instead the valid range is 0-1, then
  the variable name *may* contain the word "fraction". Variables that
  are not percentages may not under any circumstances have the word
  "percentage" in their name.

- Don't add a `Dispose` method to classes that don't need it.

- Use `TryGetValue` instead of first calling `Dictionary.ContainsKey`
  and then reading the value separate because `TryGetValue` is faster.

- When trying to save dynamic type objects, the base type that is used in
  the containing object (even if it is an interface) needs to specify the
  thrive serializer using `[UseThriveSerializer]` attribute.
  For more information see [saving_system.md](saving_system.md).

- Base method calls should be at the start of the method, unless
  something really has to happen before them. This is to make it
  easier see that the base method is called and not forgotten. Often
  it is better to call the base method even though it doesn't do
  anything to guard against future bugs (if a new sub class is added /
  the base class is changed).

- Prefer using declarations (C# 8 feature) over using statement
  blocks. If you need to reduce the scope of the using variables,
  start a new block with curly braces and use the using declaration
  inside it.

- Don't use async (C# feature). Instead use the `TaskExecutor` to run
  background tasks. Godot APIs don't use async either and none of the
  existing code does.

- Continuous Integration (CI) will check if the formatting scripts and
  tools find some problems in your code. You should fix these if your
  pull request (PR) fails the CI build.

- You should familiarize yourself with the codebase at least somewhat
  so that you can use similar approaches in new code as is used in
  existing code as new code should follow the conventions (even if not
  mentioned in this guide) that existing code has established.

- Defensive programming is recommended. The idea is to write code that
  expects other parts of the codebase to mess up somewhere. For example,
  when checking if a species is extinct, instead of checking
  `population == 0`, it is recommended to do `population <= 0` to guard
  against negative population bugs.
  
- When writing conditions checking booleans, don't explicitly write
  out `true` or `false` (unless the variable is nullable in which case
  the explicit compare is required). So write code like this: `if
  (thing)` and not: `if (thing == true)`.

- Finally you should attempt to reach the abstract goal of clean
  code. Here are some concepts that are indicative of good code (and
  breaking these can be bad code): Liskov substitution principle,
  single purpose principle, logically putting same kind of code in the
  same place, avoid repetition, avoid expensive operations in a loop,
  prefer simpler code to understand. Avoid anti-patterns, for example
  God class.

Memory allocation
-----------------

As Thrive is a game, memory usage needs to be considered relatively
carefully. Code that runs each game update, or very often, should be
designed to avoid all memory allocations. For example by storing a
temporary list of working memory that can be reused. Rarely done
operations or ones that don't happen during gameplay can be more
relaxed about memory.

Some C# features allocate memory in a hidden way. The `foreach` loop
allocates memory *if* the C# compiler cannot be sure that the iterated
object is an inbuilt `List` or `Dictionary`, iterating an interface
like `IReadOnlyList` will cause memory allocations even if the
underlying object is a list (so the actual type of the variable as
seen during compile time is what matters). If this kind of thing needs
to be looped, then a manual for-loop is required based on the length
of the list.

LINQ operarations that allocate memory, which is sadly most of them,
should be avoided in commonly running code (especially in systems that
run each frame). LINQ methods that take in a callback may allocate
surprisingly large amounts of memory due to capturing local variables.

To much more easily see where memory is being allocated, the following
plugin is very useful for Rider:
https://github.com/controlflow/resharper-heapview

Of course things like spawning new NPCs etc. can allocate new memory
as naturally things like spawning new game objects is expected to
require more memory.

Godot usage
-----------

- GUIs need to be usable with the mouse and a controller. See
  [making_guis.md](making_guis.md).

- Do not use Control offsets to try to position elements, that's not good
  Godot usage. Use proper parent container and min size instead.

- For spacing elements use either a spacer (that has a visual
  appearance) or for invisible space use an empty Control with rect
  `minsize` set to the amount of blank you want.

- Don't use text in the GUI with leading or trailing spaces to add
  padding, see previous bullet instead.

- Node names should not contain spaces, instead use PascalCase naming.

- For connecting signals, use `nameof` to refer to methods whenever possible
  to reduce the chance of mistakes when methods are renamed.

- To refer to signal *names* use `ClassName.SignalName.SignalName`

- Do not use the `+=` syntax for connecting signals unless completely
  necessary. Use `Connect` whenever possible instead. This is because
  there's a very easy to do mistake that causes signals to not be
  unregistered and disposed object exceptions when signals are
  emitted, see the warnings on the [documentation
  page](https://docs.godotengine.org/en/4.2/tutorials/scripting/c_sharp/c_sharp_signals.html). This
  rule can be relaxed once Godot properly gets [automatic
  unregistering](https://github.com/godotengine/godot/issues/70414).

- If you need to keep track of child elements that are added through a
  single place, keep them in a List or Dictionary instead of asking
  Godot for the children and doing a bunch of extra casts.

- When destroying child Nodes or Controls take care to detach them
  first, in cases that having them hang around for one more frame
  causes issues, as that doesn't happen if you just call
  `QueueFree`. You can instead call `DetachAndQueueFree`
  instead to detach them from parents automatically.

- The order of Godot overridden methods in a class should be in the
  following order: (class constructor), `_Ready`, `_ExitTree`, `_Process`,
  `_Input`, `_UnhandledInput`, (other callbacks)

- If you need to access parent objects, don't make a static public
  instance variables, instead pass callbacks etc. around to allow the
  child objects to notify the parent objects, this is to reduce
  coupling and global variables.

- To remove all children of a Node use `FreeChildren` or
  `QueueFreeChildren` extension methods.

- DO NOT DISPOSE Godot Node derived objects, call `QueueFree` or
  `Free` instead. Also don't override Dispose in Node derived types to
  detect when the Node is removed, instead use the tree enter and exit
  callbacks to handle resources that need releasing when removed.

- DO NOT DISPOSE `GD.Load<T>` loaded resources. Any calls with the
  same resource path will result in the same object instance being
  returned. So it is not safe to dispose as other users may still be
  using it.

- For scene attached Nodes, they do not need to be manually freed or
  disposed. Godot will automatically free them along with the parent.

- `NodePath` variables should be disposed as they aren't part of the
  scene tree or Godot properties it likely knows about. So disposing
  those variables will speed up their clearing.

- Automatic code checks will complain about `CA2213` due to the above.
  For the above cases use `#pragma warning disable CA2213` and
  `#pragma warning restore CA2213` around the block of variables to
  suppress the warning. The warning is not globally suppressed as
  non-Godot objects should still be disposed according to good style
  so the warning helps in catching these cases. For example many
  standard C# classes need to be disposed and for those objects, even
  when they are held by Godot objects, custom dispose methods should
  be implemented.

- For most Godot-derived types a `Dispose` method just needs to be
  added to dispose any `NodePath` variables. Note that Godot sometimes
  creates partly initialized objects (for example autoloads, when
  loading saves, and objects that Godot editor creates
  internally). For that reason the `Dispose` method needs to work even
  with those partly initialized objects. To take this into account the
  `Dispose` method should check that the `Export` variables are set
  (the first `NodePath` variable needs to be set nullable) like this:

```c#
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (FirstControlPath != null)
            {
                FirstControlPath.Dispose();
                SecondControlPath.Dispose();
                ThirdControlPathAndSoOn.Dispose();
            }
        }

        base.Dispose(disposing);
    }
```

- Avoid using a constructor to setup Godot resources, usually Node
  derived types should mostly do Godot Node related, constructor-like
  operations entirely in `_Ready`. Many resources are not ready yet when
  a class is constructed or static variables are being initialized. If
  this is not followed script variables may not show up correctly in
  the Godot editor as that relies on creating an instance of the
  class, and that can fail for example because SimulationParameters
  are not initialized in the Godot editor. This needs especial care
  when a class type is directly attached to a Godot scene.

- Regarding nullability and Node references that are null before a Node
  enters the scene. They can be used if you want to support setting a
  property that affects a child Node before the Node is added to the scene.
  For example the following shows how that can be structured:
  ```c#
  [Export]
  public NodePath SpinnerPath = null!;
  
  private TextureRect? spinner;
  
  private bool showSpinner;
  
  public bool ShowSpinner
  {
      get => showSpinner;
      set
      {
          showSpinner = value;
          UpdateSpinner();
      }
  }
  
  public override void _Ready()
  {
      spinner = GetNode<TextureRect>(SpinnerPath);
      UpdateSpinner();
  }
  
  private void UpdateSpinner()
  {
      if (spinner != null)
          spinner.Visible = ShowSpinner;
  }
  
  private void SomeOperationValidAfterReady()
  {
      // Use nullability suppression here once a method is only valid after
      // _Ready has been called
      spinner!.Something();
  }
  ```

- Related to the above point, throw `SceneTreeAttachRequired` if you write
  an operation that may not be called before `_Ready` has ran, and it's
  a public method or can be triggered that way and someone might call it 
  too early.

- You should follow general GUI standards in designing UI. Use widgets
  that are meant for whatever kind of interaction you are designing.

- We have rewritten several controls to workaround Godot bugs or limitations,
  and add custom features. All these rewritten/customized controls are placed
  in `res://src/gui_common/`. Currently there are `CustomCheckBox`,
  `TopLevelContainer`, `CustomWindow`, `CustomConfirmationDialog`, `ErrorDialog`,
  `TutorialDialog`, `CustomDropDown`, `CustomRichTextLabel`, and
  `TweakedColourPicker`. Consider using these custom types rather than the
  built-in types to ensure consistency across the GUI.

- When you are instantiating a custom Control in Godot, use
  `Instance Child Scene` if it has a corresponding scene (.tscn) file; If it
  doesn't, add a corresponding built-in Control and use `Attach Script`.
  An alternative is to locate the scene or script file in `FileSystem` panel
  (by default on the bottom-left corner) and drag it to the proper position.

- When you are instantiating a custom Control in code, use the following if it
  has a corresponding scene (.tscn) file; use `new T` if it doesn't.
  ```C#
  var scene = GD.Load<PackedScene>("res://src/gui_common/T.tscn");
  var instance = scene.Instance<T>();
  ```

- Question popups should have a short title ending in a question mark
  (`?`). The content of the popup should give more details and also
  end with a question.

- Popups (which derives from `TopLevelContainer`) should be shown with
  `PopupCenteredShrink()`. However, if you don't wish to center the popup,
  simply use `TopLevelContainer.OpenModal()`.

- Using built-in `Popup` is not recommended since a custom one tailored
  for the game already exist but for posterity similar rules in
  the above point still stands. In addition, don't use `Popup.Popup_`,
  instead prefer to use `Popup.Show` or `Popup.ShowModal`, only if those
  don't work then you can consider using `Popup.Popup_`.

- Don't use `Godot.Color(string)` constructor, unless explicitly
  needed. An explicit need is for example loading from JSON or from
  user input. String literal colours should not be used in C# source
  code.

- When using fonts, don't directly load the .ttf file with an embedded
  font in a scene file. Instead create a label settings in
  `src/gui_common/fonts` folder and use that. This is needed because
  fonts need to have settings like fallback fonts set, for example
  Chinese. All fonts should be TrueType (`.ttf`) and stored in
  `assets/fonts`. For variable weight fonts the variants created from
  the font should be placed in `assets/fonts/variants`. For buttons
  that cannot use label settings, it is preferrably to just set a
  theme font size override, but when really needed the override font
  can be set, but care needs to be taken that this points to a proper
  font. For variable weight fonts only the variants should be used and
  not the base font directly.

- All images used in the GUI should have mipmaps on in the import
  options.

Other recommended approaches
----------------------------

- When changing the meaning of a game setting in a major way that is
  incompatible with previous values, the updated setting should use a
  different name when saved in JSON to avoid problems. For example:
  `[JsonProperty(PropertyName = "MaxSpawnedEntitiesV2")]`. This way
  the options menu doesn't need complicated adapting logic as
  otherwise it would show misleading values to the player.

- When using new words that would be detected as typos by Rider, the
  words should be added to the project dictionary. The dictionary is
  in `Thrive.sln.DotSettings` file. For example if "florbing" was a
  valid new concept that needed to be used in the code, it could be
  added by adding the following line next to the other dictionary
  entries: `<s:Boolean
  x:Key="/Default/UserDictionary/Words/=florbing/@EntryIndexedValue">True</s:Boolean>`

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

Gameplay changes
----------------

When doing changes that impact existing gameplay or add new gameplay
additional considerations regarding playability and understandability
need to be taken into account.

- When changing an existing mechanic that has tutorials, tooltips, help 
  menu entries or other explanations, you must also update those texts
  so that how we explain the game to the player doesn't get out of sync.
  This is because if a gameplay changing PR is accepted it may take multiple
  months for anyone to bother to update the help text meanwhile players don't
  know about the new mechanics.

- For new gameplay features it is recommended but not mandatory to write
  new help text or tutorials to explain them.

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

- Once you get invited to the main repository, you should make your
  feature branches exclusively in that (and not use a fork). This is
  so that it is easier for other team members to checkout your code
  and help with it by committing to your branch.

- If you haven't made many pull requests in the past, it is highly
  recommended to keep anything not directly needed in your PR away
  from it to make reviewing easier. Adding refactoring (other than
  refactoring that is asked for by an open issue) or style changes in
  your first PR will make it more of a hassle to get it accepted.

- If you are working on a GitHub issue, your feature branch's name
  should begin with the issue number, followed by an underscore,
  followed by a short, descriptive name in
  `lower_case_with_underscores`. The name should be short, but
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
  directly on Github if there aren't any conflicts. If you want, you
  can alternatively rebase the feature branch onto master, but this is
  not required normally. In a case where a direct merge (not squash)
  is wanted to master then a rebase needs to be performed to clean up
  the commits in the feature branch.

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

- An exception to the above rule are the automatic PRs from
  [Weblate](https://translate.revolutionarygamesstudio.com/), those
  must be merged normally, otherwise weblate won't detect that
  correctly and fixing that requires a lot of manual merging. So merge
  weblate PRs as separate commits, not squashed. If there is a merge
  conflict with a Weblate PR or Weblate can't rebase on latest master
  due to a conflict, a manual merge is needed. For this checkout
  latest master locally, then fetch the Weblate remote to get the
  latest code, and finally just merge `weblate/master` to `master` and
  push the result to `origin/master`.

- You should not leave the co-authored-by line in a squashed commit if
  all that person did was merge master into the branch to make the
  merge show up as green on Github or if it is a really tiny
  suggestion like a simple typo fix.

- For maintainers: When manually merging (which is something you
  should avoid) GitHub requires a merge commit to recognize the
  merging of a pull request. A "git merge --squash" does not create a
  merge commit and will leave the pull request's branch "dangling". To
  make GitHub properly reflect the merge, follow the procedure
  outlined in the previous bullet point, then click the "Merge Pull
  Request" button on GitHub (or do a normal "git merge").

- All team members are encouraged to review pull requests. However,
  you shouldn't go and merge PRs if you haven't discussed it with the
  programming team lead yet / it isn't approved by them yet. Another
  person with access can perform PR merges if the programming team
  lead is not available, but in this case another team member should
  also review the code beforehand.
