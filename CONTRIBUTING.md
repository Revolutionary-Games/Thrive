# Contributing to Thrive

This is a guide for contributing to Thrive.

__Note: if you have an idea, share it on our [community
forums](https://community.revolutionarygamesstudio.com/) or check out
and vote on our [suggestions
site](https://suggestions.revolutionarygamesstudio.com/)!__ and don't
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

Then follow the standard fork workflow:
https://gist.github.com/Chaser324/ce0505fbed06b947d962 to make the
required changes to fix the issue. You should check the documentation
in the "doc" folder as well as check Godot's
[documentation](https://docs.godotengine.org/en/stable/). Note that
you should check the [setup instructions](doc/setup_instructions.md)
for which version of Godot you need, as well as the version of the
Godot documentation you should read. As an introduction to Thrive
code, you can read [architecture.md](doc/architecture.md).

Please follow our [styleguide](doc/style_guide.md) when making your
changes to Thrive. Note that our styleguide has a section on Git
usage, which you should read!

Once your changes are complete, then open a pull request (PR) to this
repo and someone from the team will review your pull request. When
making changes in existing code you **must** read the comments on the
methods you modify, paying special attention to any comments really
close to your code change. This is because otherwise you will cause
bugs by not taking edge cases that existing methods have been coded
with in mind. Also you need to update any comments that are no longer
accurate after your changes. Otherwise the code base will become a
mess so we can't let this kind of sloppy code changes through reviews.

Note that currently it is not possible commit changes to Git LFS if
you are not a team member, so you need to ask someone from the team
for help if your PR includes changes to assets.

When opening a pull request, please use the closes or fixes keywords
to mark the issues the PR closes. For example `closes #2000`. When the
PR is then merged the linked issues are automatically closed.

If there is a problem in a PR, please include new commits to the same
branch (PR). You can even force push to remove every single commit in
the PR and replace them with different ones. So there is no reason to
close a PR if changes are needed. Other than if you no longer plan to
finish it.

Before marking your PR as ready for review (not a draft) please work
through the PR testing checklist to not waste the reviewer's time if
they have to find basic issues in your PR:
https://wiki.revolutionarygamesstudio.com/wiki/Testing_Checklist

Requirements in Github issues are often vague, so please feel free to
ask for more details in the issue. If you contribute a feature or fix
that doesn't have an open issue, or a [dev
forum](https://forum.revolutionarygamesstudio.com/) discussion, we may
make a lot of gameplay related change requests before accepting. See
the next paragraph for what you need to do if we won't otherwise
accept your contribution. However, if the change you are making has many votes
on [our suggestions
site](https://suggestions.revolutionarygamesstudio.com/) that'll make
it much more likely to be accepted.

If you want to contribute a non-planned feature, then you must add
code to disable your changes. Note: currently we don't have an options
menu that could be used to enable inbuilt mods, so this is a bit
difficult at the time of writing.

If your PR breaks save compatibility (older saves no longer being
loadable) you should include a save upgrader in your PR. Note that you
may need to introduce a new sub version / bump the version number to
make it possible to trigger the save upgrader.

If your PR requires new translations or touches a part that uses translations,
please read the translations documentation linked in the next section.

## Translating the game

You can find the necessary information about how to translate the game [here](doc/working_with_translations.md).

## Planning Board

The planning board contains all issues and pull requests grouped
by their priority and status. It can be found [here](https://github.com/orgs/Revolutionary-Games/projects/2).

We have a separate planning board for only graphics tasks for artist
to find things to work on:
https://github.com/orgs/Revolutionary-Games/projects/4

## Continuous integration checks

We use continuous integration (CI) systems to run automatic checks on
code before accepting it. After making a pull request please make sure
the CI jobs finish correctly on your code and fix any style
etc. errors they detect. Note that sometimes the CI jobs can
sporadically fail without you being at fault. If you are unsure if
that's the case (even after using the code checks locally), you can
ask someone from the team to look at the situation.

To view CI build logs and other information, press the "details" button on Github in the 
checks section.

<img src="https://randomthrivefiles.b-cdn.net/setup_instructions/images/viewing_ci_results.png" alt="rider godot plugin">

When updating Godot import settings (meaning changing the settings for
an existing asset, if you just added a new one, you don't need to do
this), or adding new C# dependencies, you should update the cache
versions for CI. To do this you need to increment the relevant numbers
by 1 in `CIConfiguration.yml`. When updating the caches you must also
remember to update the `writeTo` cache. Otherwise caching will not
work correctly. If you are unsure which cache names to increment,
please ask.

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
