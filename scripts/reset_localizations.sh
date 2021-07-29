#!/bin/bash
echo "This will reset all translations made in this branch."
echo "Run this script from the root Thrive folder."
read -p "Are you sure you want to continue? (Y/N)" -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    if test -f "Thrive.sln"; then
        currentBranch=`git rev-parse --abbrev-ref HEAD`
        git stash
	git checkout master
        git pull
        git checkout ${currentBranch}
        git stash pop
	git checkout master locale/
        ruby scripts/update_localization.rb
	poedit locale/en.po || $(git config --global core.editor) locale/en.po || $(git config --global core.visual) locale/en.po || vi locale/en.po
    else
        echo "I told you to run this script from the root Thrive folder!!"
    fi
fi
