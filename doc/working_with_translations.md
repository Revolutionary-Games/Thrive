Working with translation
========================

Thrive is using gettext as a localization tool. gettext works by having a
template file (of extension type .pot) and then one translation file for
each language (of extension type .po).

Online
------

There is an online translation site for Thrive available 
[here](https://translate.revolutionarygamesstudio.com). You can use that site
with just a web browser (after registering) in order to provide translations
to the game.

<a href="https://translate.revolutionarygamesstudio.com/engage/thrive/">
<img src="https://translate.revolutionarygamesstudio.com/widgets/thrive/-/open-graph.png" alt="Translation status" width="600px"/>
</a>

This has the limitation that you can't test how the translations look in the game.
But if you want to easily just help out a bit with the translations, that's the
perfect place to do so. You can continue reading this document if you are interested
working more in-depth with the translations for the game.

Required tools
--------------

The list of tools needed for localization can be found on the
[Setup instruction page](setup_instructions.md#Localization-tools).

Adding new content into the game with translation in mind
---------------------------------------------------------

Working with translation in mind will be a bit different than usual, as
it will require a few more steps when working with strings.

### Working in scene files

When working on a scene, once you are done designing it, take note
of all the strings (text, titles, ...) somewhere.

Replace all the strings in your scene with keys (eg. AUTOEVO_POPULATION_CHANGED) 
and "match" them with your strings.

(You can use a simple text file writing str => key, or anything else you prefer)

If you include placeholder strings that are meant to make designing or debugging easier,
you can add PLACEHOLDER as the editor description for the Node that contains that text. This
will make the translation system skip it.

### Working in C# files

Always call `TranslationServer.Translate()` for strings that should be localized.

Other than that, it is the same principle has for the scene files:
once you are done, write down your strings somewhere And change them in the code into keys.

Note that due to the way the text extraction works, only string
literals work in the `Translate` call, using variables or string
concatenation, won't extract things properly. For example this is the
correct usage: `TranslationServer.Translate("A_TRANSLATION_KEY");`

The translation keys need to be named all uppercase with underscores
(`_`) used to separate words. If a general name (that may be used in
multiple places) is used in a translation key, and there is
punctuation after it, the key should have a `_DOT` or `_COLON` or
whatever the punctuation is as a suffix.

Generally, general translation keys should be used so that they can be
used in many different contexts to reduce the required translation
effort. Note that some languages can't use the same word as in English
in different context, so the translation keys should be context
specific. For example different keys should be used for the word
"play" when used in music playing context and when used in game
playing context. In contexts where general names are not good, for
example in the previous example, the context should be included in the
translation key like `PLAY_MUSIC`.

### Updating the localizations

Once you are done adding content into the game, go into the scripts folder and
run `update_localization.rb`. This will extract the strings from the game files,
and also update the .po files if the template (.pot) has changed.

The final step is to open en.po in the locale folder (you can use a text editor
or Poedit), search for your keys, and add your strings as translation. Once done,
you can launch the game and make sure everything works as expected.

Note that you should configure your gettext tool to not use line
wrapping. We don't use line wrapping because Weblate and gettext
command line tools disagree where lines should be wrapped in many
cases, so we don't use that to reduce cases where translations are
automatically changed back and forth to different line wrapping
lengths.

### How the translations work

This section is a brief overview on how the Godot translation system works, for more
info you can read Godot's documentation on it. This section also briefly covers our
extensions to Godot regarding translations. Read this to save your and code reviewer's
time if you are working on code that has an impact on translations.

As mentioned earlier in this document the key concept in working with translations
are the translation keys, for example `PLAY_MUSIC`. When `TranslationServer.Translate(string)`
is called it will look through the loaded translations (`project.godot` file 
defines the translation files to load) to find if there exists a translation for the
currently active language with the specified key. If such translation is not found
(or it is marked as "fuzzy" / needing changes) then a translation is searched in the
English locale with that key. 

If a translation is found then the call to `Translate`
returns that translation. However, if nothing is found the key is returned as is.
This is usually bad and points to translations not being up to date / the English
translation missing a translation for something. In some cases though strings that
are already translated or don't contain translatable content may be passed through
the translation system, but this should be usually avoided as unnecessary work. There
may be some places where translatable and untranslatable content can be shown in the same
place with complicated logic where the simplest solution is to pass all text through the
translation system so that the things that need translating get translated and other strings
just pass through.

Our custom extensions to the Godot translation system consist of two classes:
`LocalizedString` and `LocalizedStringBuilder`. `LocalizedString` is a special
type of string that allows passing in the translation key to it. For example:
`new LocalizedString("PLAY_MUSIC")`. When the `LocalizedString` is converted to a
normal string, that is when it automatically passes the key it was passed to
the translation system. This means that `LocalizedString` and the builder variant
(for building strings out of segments) **should not** be converted to a string any
earlier than just when passing to the GUI for display. Otherwise the string can no
longer react to translation changes. The GUI handling class should react to 
`NotificationTranslationChanged` events in their `_Notification` method, and reapply
the text to the GUI from the `LocalizedString` objects. This way the game can immediately
react to the user changing the selected language. For example auto-evo results text goes 
as far as to even write the `LocalizedStringBuilder` object to the game saves so that language
can be changed after loading a save and the results text will still update correctly.

A more advanced use case of `LocalizedString` is when placeholders are used. Placeholders
in strings are `{0}`, `{1}`, etc. which will be replaced with other text when the localized
string is converted to a normal string. Note that as the key of a translation is passed to
the `LocalizedString` constructor, you need to have a normal key for the text and *then*
in the English *translation* you need to write text like `This thing: {0}`. `LocalizedString`
can also be nested (also works with `LocalizedStringBuilder`) as string placeholders. For
example here's a complex use example:
```csharp
var localized = new LocalizedString("MY_KEY", 1234, new LocalizedString("MY_OTHER_THING"));
GD.Print(localized);
```

And if the English translation file has:
```
msgid "MY_KEY"
msgstr "My things are {0} and {1}"

msgid "MY_OTHER_THING"
msgstr "important stuff"
```

the code example will print to Godot logs: `My things are 1234 and important stuff`. And
as long as the localized string instance is kept around the final text can be generated
again and again when the game language changes and it will function properly. So doing
something like (and storing the result for use later): 
`var myVariable = localized.ToString();` is bad as the text can no longer react to language
changes.

Translating the game into a new language
----------------------------------------

### Only the first time: create your locale .po file

To create a new .po file for your localization,
you have two choices: using the commands, or Poedit.

#### With commands

Execute the following command in the locale folder:

```sh
msginit --no-translator --input=messages.pot --locale=LANGUAGE_CODE_HERE,
```

#### With Poedit

Open Poedit and use it to generate the .po file by going into the menu
File/New from POT/PO file...

**In both cases, you'll need to enter your language code. You can find a list of all
supported code [in the Godot engine documentation](https://docs.godotengine.org/en/stable/tutorials/i18n/locales.html)**

#### Add your .po file to the update script

To make updating the localization easier, you should add a reference to
the new .po file into `scripts/update_localization.rb`.

Simply open the ruby script into any text editor, and edit the locale list as such:

```ruby
LOCALES = %w[en fr _new-locale_].freeze
```

For example:

```ruby
LOCALES = %w[en fr jp].freeze
```

**If you are not confident in doing it, you can always ask for a programmer to do it for you 
in your pull request.**

### Translate the text

Now that you have the .po file used for the new localization created, you can translate
the game. Simply open the file with Poedit or a simple text editor and translate the text.
Since we are working with keys, you'll want to open en.po on the side too and use the English 
game text there as a reference.
