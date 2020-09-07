pybabel extract -F babelrc -k LineEdit -k text -k window_title -k dialog_text -o messages.pot ../.

msgmerge --update --backup=none en.po messages.pot
msgmerge --update --backup=none fr.po messages.pot
