#!/bin/bash
pipenv run pybabel extract -F babelrc -k text -o l10n.pot .
msginit --no-translator --input=l10n.pot --locale=ru
