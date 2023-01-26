Making Graphical User Interfaces
================================

Thrive is first meant to be playable with a keyboard and a mouse so
any new GUIs or changed GUIs must be usable with mouse
input. Controller support is also a priority meaning that any existing
GUIs may not be broken by changes for a controller. And any new GUIs
for non-prototype parts of the game need to be at least usable with a
controller to allow the user to exit that GUI and not get trapped.

If you do not have a controller to test, you can alternatively use
keyboard navigation (arrow keys, tab, shift+tab) to test that
navigation works.

Many tiny specific rules for GUIs are mentioned in the
[styleguide](style_guide.md).

Focus is Needed
---------------

In each GUI view there always must be an element that gets focused by
default, otherwise the navigation flow entirely breaks as if there is
nothing focused the controller navigation buttons do nothing.

An easy way to fix this is to use the `FocusGrabber.tscn` child scene,
which allows configuring a Control to give focus when the grabber is
visible. Multiple of these can be placed in a scene to make it so that
when parts are hidden and others are shown, there's always an active
grabber that makes sense.

For a Control to be focusable it needs to be set to accept "all" focus
instead of "none"

Navigation Directions
---------------------

Quite often the default navigation directions and next/previous
Control don't make sense. In these cases the navigation directions
need to be manually set.

Usually the next and previous Controls should be set to a same value
as in one of the navigation directions.

Always an Exit
--------------

Even if a GUI is not made to be controller navigable, there should at
least be an exit button that is made focused by a `FocusGrabber` (and
it should have all the navigation directions set to itself to avoid
navigating away from it). This way a controller user can at least
exit the screen without having to force close the game or reach for a
mouse.

Making Tabs
-----------

When making tabs use the `TabButtons.tscn` child scene to hold the tab
buttons. This automatically adds controller navigation to the tab
buttons.

When multiple tabs are visible at once, one needs to be the primary
level and the second the secondary level. This sets different
controller buttons for switching between the tabs.

With 3 or more tab levels things get much more complicated so that
many tab levels should be avoided if at all possible.
