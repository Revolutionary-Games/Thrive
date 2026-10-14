Fixing Translation Conclicts
============================

To quickly fix translation merge conflicts do the following procedure:
- `git fetch origin`
- Make a copy of the changed translations `locale/en.po` -> `en.po.bak`. English is the main language so it can be usually assumed that no other language has text set yet for a new feature at this point.
- Reset all the localization files: `git checkout master -- locale`
- Create a commit with the message "Reset translations to prepare merge"
- Then merge "master" (or `origin/master` if local master branch is not up to date) into the current branch which should work cleanly. Abort the procedure if the merge is not successful.
- Run the localization script: `dotnet run --project Scripts -- localization`
- Then check the updated `locale/en.po` and copy all missing (`fuzzy`) translations from `en.po.bak` into the main `en.po` file.
- Finally commit the changes to the locale folder with the message "Redid translations after merge"
- As a last sanity check, the check script can be ran to make sure the translation files are logically fine: `dotnet run --project Scripts -- check localization`

