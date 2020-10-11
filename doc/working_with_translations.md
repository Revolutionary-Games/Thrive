Working with translation
========================

Thrive is using gettext as a localization tool. gettext works by having a
template file (of extension type .pot) and then one translation file for
each language (of extension type .po)

Required tools
--------------

If you are planning to do anything that would require translation, or
simply to translate the game into your locale language you may need
a few more tools.

Note if you are simply planning to add a new localization, Poedit is
enough.

### Python 3

The tool used to extract strings from the game files is using
[Python 3](https://www.python.org/downloads).
You'll need it if you are planning to add or edit strings in the game.

*Note : Linux users should already have it installed.
You can use the command **python --version** to make sure you have it.
If not, you can use the package manager of your distribution to install the **python3** package.*

### Babel and Babel_thrive

Babel and its extension [Babel_thrive](https://github.com/westtunger/pybabel-godot)
are the tools used for extracting strings from the game files.
Just like Python, you'll want to download these if you are planning
to add or edit strings into the game.

You can quickly install these by using the command **pip install Babel PyBabel-json Babel-Thrive** or **pip3 install Babel PyBabel-json Babel-Thrive**
if you have python installed.

### Optional : Poedit

[Poedit](https://poedit.net/) is a free .pot and .po file editor that may
make your life easier when working on translation files.

It is needed to create new .po files without using the command line tools.

Note: Poedit will complain about translation format since it was made to
directly use text as keys. Those can be ignored.


Adding new contents into the game with translation in mind
----------------------------------------------------------

Working with translation in mind will be a bit different than usual, as
it will require a few more steps when working with strings.

### Working in scene files

When working on a scene, once you are done designing it, take note
of all the strings (text, titles, ...) somewhere.

Replace all the strings in your scene with keys (eg. AUTOEVO_POPULATION_CHANGED) and "match" them with your
strings.

(You can use a simple text file writing str => key, or anything else you prefer)

### Working in cs files

Always call TranslationServer.Translate() for strings that should be localized.

Other than that, it is the same principle has for the scene files :
once you are done, write down your strings somewhere And change them in the code into keys.

### Updating the localizations

Once you are done adding content into the game, go into the scripts folder and
run "update_localization.rb". This will extract the strings from the game files,
and also update the .po files if they have been added to it.

The final step is to open en.po in the locale folder (you can use a text editor
or Poedit), search for your keys, and add your strings as translation.  Once done,
you can launch the game and make sure everything works as expected.

Translating the game into a new language
----------------------------------------

### Only the first time: create your locale .po file

To create a new .po file for your localization,
you have two choices: using the commands, or Poedit.

#### With commands

Execute the following command in the locale folder :

msginit --no-translator --input=messages.pot --locale=language_code,

#### With Poedit

Open Poedit and use it to generate the .po file by going into the menu
File/New from POT/PO file...

**In both cases, you'll need to enter your language code. You can find a list of all
supported code [in the Godot engine documentation](https://docs.godotengine.org/en/stable/tutorials/i18n/locales.html)**

#### Add your .po file to the update script

To make updating the localization easier, you should add a reference to
the new .po file into update_localization.rb.

Simply open the ruby script into any text editor, and edit the locale list as such :

**LOCALES = %w[en fr _new-locale_]**

E.g. **LOCALES = %w[en fr jp]**

### Translate the texts

Now that you have the .po file used for the new localization created, you can translate
the game. Simply open the file with Poedit or a simple text editor and translate the texts.
Since we are working with keys, you'll want to open en.po on the side too and use the texts
there as a reference.