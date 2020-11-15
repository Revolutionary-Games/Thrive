# Contributing to Thrive

This is a guide for contributing to Thrive.

__Note: if you have an idea, share it on our [community
forums](https://community.revolutionarygamesstudio.com/)!__ and don't
create an issue about it.

## Github Issues

Issues on our Github are not meant for general feedback. We only
accept bug reports about something not working. Team members after
consulting other team members are allowed to create issues about new
or improved features. If someone else creates such an issue, that
wasn't planned, it will be closed.

## Pull requests

First select an issue to work on. We have some issues tagged as good
first issue. You can also check the issues tagged as easy to find
something to start with. If you don't know how some issue should be
implemented feel free to ask on the issue.

Then follow the the standard fork workflow:
https://gist.github.com/Chaser324/ce0505fbed06b947d962 to do the
required changes to fix the issue. You should check the documentation
in the "doc" folder as well as check Godot's
[documentation](https://docs.godotengine.org/en/stable/). Note that
you should check the [setup instructions](doc/setup_instructions.md)
for which version of Godot you need, as well as the version of the
Godot documentation you should read.

Please follow our [styleguide](doc/style_guide.md) when making your
changes to Thrive.

Once your changes are complete, then open a pull request to this repo
and someone from the team will review your pull request. Note that
currently it is not possible commit changes to Git LFS if you are not
a team member, so you need to ask someone from the team for help if
your PR includes changes to assets.

When creating a pull request, include the "closes" or "fixes" keyword followed
by the issue number that will be closed when the pull request is
accepted. Example: `closes #1234`.

If you need to alter code for an issue, don't create a new pull request.
Existing pull requests can be updated. Simply push further commits to
the same branch.

Unfortunately, Github issues are often created quickly with little detail
and context. Please do not hesitate to ask questions regarding the
issue for clarification and details.

If you want to contribute a non-planned feature, then you must add
code to disable your changes. Note: currently we don't have an options
menu that could be used to enable inbuilt mods, so this is a bit
difficult at the time of writing.

## Translating the game

You can find the necessary informations about how to translate the game [here](doc/working_with_translations.md).

## Planning Board

The planning board contains all issues and pull requests grouped
by their priority and status. It can be found [here](https://github.com/orgs/Revolutionary-Games/projects/2).

## Getting help

If you have a question about an issue on how it should be solved,
please post your question on the issue itself.

If you need more general help check this category on our forums:
https://community.revolutionarygamesstudio.com/c/dev-help

## Joining the team

If you want to join the Revolutionary Games team, see this page:
https://revolutionarygamesstudio.com/get-involved/

You can join the team by making a non-trivial pull request that gets
accepted. Team members will be given write access to this repo once
they have done one accepted pull request.
