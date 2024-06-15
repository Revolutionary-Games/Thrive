The input system
================

This document describes the custom input system in Thrive.
The system uses C# Attributes attached to methods to call the correct code when the user does
an input action.

How to use attributes
---------------------

To apply an attribute to a method, you need to add a code line like this before a method: 
```csharp
[OneOfTheInputAttributes(parameters)]
```

The different available attributes and their parameters are described below.


You can read more about C# attributes 
[here](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/attributes/).

The available input attributes
------------------------------

| Attribute | Description | Parameters | Method parameters | Multiple |
| --------- | ----------- | ---------- | ----------------- | -------- |
| RunOnKey  | Fires repeatedly when the input is held down | input : string | delta : double | yes |
| RunOnKeyChange | Fires once when the input is pressed or released | input : string | state : bool | yes |
| RunOnKeyDown | Fires once when the input is pressed | input : string | none | yes |
| RunOnKeyUp | Fires once when the input is released | input : string | none | yes |
| RunOnKeyToggle | Fires once when the input is pressed and provides an alternating bool | input : string | state : bool | yes |
| RunOnAxis | Fires repeatedly when one of the axis members is held down. Every axis member has a value associated with it. The average of the pressed values is given to the method | inputs : string[]<br> values : float[] | delta : double<br> value : float | yes |
| RunOnAxisGroup | Combines multiple RunOnAxis. Used when you want to combine multiple axes and want to differentiate between them | none | delta : float<br>value1 : float<br>value2 : float... | no |

- **Attribute** is the name of the attribute
- **Parameters** are the parameters you have to provide to the attribute
- **Method** parameters are the parameters the method you are applying this attribute to has to have. You can name the parameters however you want.
- **Multiple** describes if you can attach this attribute to the same method multiple times.

Input receiving instance management
-----------------------------------
If the method is **static**, you don't have to worry about instance management.

If the method is **not static** you must do one of the following:

- inherit from `NodeWithInput` if your class needs to inherit from `Godot.Node` **or**
- inherit from `ControlWithInput` if your class needs to inherit from `Godot.Control` **or**
- add `InputManager.RegisterReceiver(this)` to your `\_EnterTree` and `InputManager.UnregisterReceiver(this)` to your `\_ExitTree`

InvokeAlsoWithNoInput
---------------------

`RunOnAxis` and `RunOnAxisGroup` both have the `InvokeAlsoWithNoInput` property.

You can set that property like this: 
```csharp
[RunOnAxisGroup(InvokeAlsoWithNoInput = true)]
```
or
```csharp
[RunOnAxis(InvokeAlsoWithNoInput = true)]
```

By default this property is false.

This property defines if the method should be called with the default value even when no axis 
input is pressed. The default value is the average of all given values.

If `RunOnAxisGroup` attribute is found on a method, all other `InvokeAlsoWithNoInput` 
values are ignored as the axis group overwrites them.

OnlyUnhandled
-------------

Every `InputAttribute` has the `OnlyUnhandled` property.

By default this property is true.

This property defines if the method should be called even if the Input was already marked as handled.

Priority
--------

Every `InputAttribute` has the `Priority` property.

By default this property is 0.

This property defines which method gets called if two methods have the same Action attached to them.
The method with the lower priority only gets called if there is no instance attached to the method with the higher priority or no instance returned true.

Examples
--------

```csharp
public class FPSCounter : ControlWithInput
{
  [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false)]
  public void ToggleFps() {}
}
```

The ToggleFps method gets called whenever the `toggle_FPS` action is pressed (determined by 
the current key bindings).
The ToggleFps method even gets called if something else already consumed this event.

---

```csharp
public class FPSCounter
{
  [RunOnKeyDown("toggle_FPS", OnlyUnhandled = false)]
  public static void ToggleFps() {}
}
```

This method also gets called whenever the `toggle_FPS` action gets pressed. 
Notice that no inheritation is required.

---

```csharp
public class MicrobeCamera : Camera
{
  public override void _EnterTree()
  {
    InputManager.RegisterReceiver(this);
    base._EnterTree();
  }

  public override void _ExitTree()
  {
    InputManager.UnregisterReceiver(this);
    base._ExitTree();
  }

  [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f })]
  public void Zoom(double delta, float value) {}
}
```

The Zoom method gets called when `g_zoom_in` or `g_zoom_out` gets pressed. 
Via the `value` parameter you know if the user pressed wants to zoom in or out.

The `-1.0f` belongs to the `g_zoom_in` and the `1.0f` belongs to the `g_zoom_out`.

Because there is no input class ready for Camera we have to write the logic ourself.

---

```csharp
public class MicrobeCamera
{
  [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f })]
  [RunOnAxis(new[] { "g_zoom_in_fast", "g_zoom_out_fast" }, new[] { -3.0f, 3.0f })]
  public void Zoom(double delta, float value) {}
}
```

The instance management is omitted in this example.

This example is valid as well. Using the value you know in which direction and how fast 
you should zoom the camera

---

```csharp
public class MicrobeCamera
{

  [RunOnAxis(new[] { "g_zoom_in", "g_zoom_out", "g_zoom_in_fast", "g_zoom_out_fast" }, 
      new[] { -1.0f, 1.0f, -3.0f, 3.0f })]
  public void Zoom(double delta, float value) {}
}
```

This example is almost the same as the example above. The only difference is when pressing 
inputs like for example `g_zoom_out` and `g_zoom_in_fast` together.

Using the last example the method will get called two separate times, once with 
`1.0f` and once with `-3.0f`. Using this example the method will get called only once with 
`-1.0f` (the average of 1.0f and -3.0f).

With a correct implementation utilizing delta, this difference should not matter.

---

```csharp
public class PlayerMicrobeInput : NodeWithInput
{
  [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
  [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
  public void OnMovement(double delta, float value) {}
}
```

This example would theoretically work, but would not make sense, because you cannot 
differentiate between forward/backward input and left/right input.

The correct way of implementing this would be this:
```csharp
public class PlayerMicrobeInput : NodeWithInput
{
  [RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
  [RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
  [RunOnAxisGroup]
  public void OnMovement(double delta, float forwardBackwardMovement, float leftRightMovement) {}
}
```

Using `RunOnAxisGroup` you can differentiate between forward/backward input and left/right 
input.

---

```csharp
public class FPSCounter : ControlWithInput
{
    [RunOnKeyToggle("toggle_FPS", OnlyUnhandled = false)]
    public void ToggleFps(bool state)
    {
        Visible = state;
    }
}
```

This example allows you to toggle the visibility of a control each time the user presses one button.
