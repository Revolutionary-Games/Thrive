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

### Updating the localizations

Once you are done adding content into the game, go into the scripts folder and
run `update_localization.rb`. This will extract the strings from the game files,
and also update the .po files if the template (.pot) has changed.

The final step is to open en.po in the locale folder (you can use a text editor
or Poedit), search for your keys, and add your strings as translation.  Once done,
you can launch the game and make sure everything works as expected.

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
