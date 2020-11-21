The input system
====================
This document describes the new attribute based user input system.<br>
The current system uses C# Attributes attached to methods to call the correct code when the user fires an input action.

How to use attributes
-----------
To apply an attribute to a method, you need to add a code line like this over a method: `[OneOfTheInputAttributes(parameters)]`<br>
The different avaible attributes and their parameters are described below.<br>
You can read more about C# attributes [here](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/attributes/).

The avaible input attributes
-----------
| Attribute | Description | Parameters | Method parameters | Multiple |
| --------- | ----------- | ---------- | ----------------- | -------- |
| RunOnKey  | Fires repeatedly when the input is pressed | input : string | delta : float | yes |
| RunOnKeyChange | Fires once when the input is pressed or released | input : string | none | yes |
| RunOnKeyDown | Fires once when the input is pressed | input : string | none | yes |
| RunOnKeyUp | Fires once when the input is released | input : string | none | yes |
| RunOnAxis | Fires repeatedly when one of the axis members is pressed<br>Every axis member has a value associated with it.<br>The average of the pressed values is given to the method | inputs : string[]<br> values : float[] | delta : float<br> value : float | yes |
| RunOnAxisGroup | Combines multiple RunOnAxis<br>Used when you want to combile multiple axes and want to differenciate between them | none | delta : float<br>value1 : float<br>value2 : float... | no |

- **Attribute** is the name of the attribute
- **Parameters** are the parameters you have to provide to the attribute
- **Method** parametes are the parameters the method you are applying this attribute to has to have. You can name the parameters however you want.
- **Multiple** describes if you can attach this attribute to the same method multiple times.

Instance management
-----------
The input system has to know for with which instances the method should be called with.<br>
If the method is **static**, you don't have to worry about instance management.<br>
If the method is **not static**, you need to add `InputManager.AddInstance(this);` into the constructor of you class to subscribe this instance to the input system.<br>
To unsubsribe an instance from the input system call `InputManager.RemoveInstance(this);`. An instance automatically gets unsubscribed if it got disposed by the garbage collector.

InvokeWithNoInput
-----------
`RunOnAxis` and `RunOnAxisGroup` both have the `InvokeWithNoInput` property.<br>
You can set the property like this: `[RunOnAxisGroup(InvokeWithNoInput = true)]` or `[RunOnAxis(InvokeWithNoInput = true)]`<br>
Per default this property is false.<br>
This property defines if the method should be called with the default value even when no axis input is pressed.<br>
The default value is the average of all given values.<br>
If `RunOnAxisGroup` is found on the method, all `RunOnAxisGroup.InvokeWithNoInput` values are ignored

Examples
-----------
```
public FPSCounter()
{
    InputManager.AddInstance(this);
}

[RunOnKeyDown("toggle_FPS")]
public void ToggleFps() {}
```
The ToggleFps method gets called whenever the `toggle_FPS` action (F3) gets pressed.
<hr>

```
[RunOnKeyDown("toggle_FPS")]
public static void ToggleFps() {}
```
This method also gets called whenever the `toggle_FPS` action gets pressed. Notice that no instance management is required.
<hr>

```
public MicrobeCamera()
{
    InputManager.AddInstance(this);
}

[RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f })]
public void Zoom(float delta, float value) {}
```
The Zoom method gets called when `g_zoom_in` or `g_zoom_out` gets pressed. Via the `value` parameter you know if the user pressed wants to zoom in or out.<br>
The -1.0f belongs to the g_zoom_in and the 1.0f belongs to the g_zoom_out
<hr>

```
public MicrobeCamera()
{
    InputManager.AddInstance(this);
}

[RunOnAxis(new[] { "g_zoom_in", "g_zoom_out" }, new[] { -1.0f, 1.0f })]
[RunOnAxis(new[] { "g_zoom_in_fast", "g_zoom_out_fast" }, new[] { -3.0f, 3.0f })]
public void Zoom(float delta, float value) {}
```
This example is valid as well. Using the value you know in which direction and how fast you should zoom the camera
<hr>

```
public MicrobeCamera()
{
    InputManager.AddInstance(this);
}

[RunOnAxis(new[] { "g_zoom_in", "g_zoom_out", "g_zoom_in_fast", "g_zoom_out_fast" }, new[] { -1.0f, 1.0f, -3.0f, 3.0f })]
public void Zoom(float delta, float value) {}
```
This example is almost the same as the example above. The only difference is when pressing inputs like for example `g_zoom_out` and `g_zoom_in_fast` together.<br>
Using the last example the method will get called two seperate times, once with **1.0f** and once with **-3.0f**<br>
Using this example the method will get called only once with **-1.0f** (the average of 1.0f and -3.0f)<br>
With a correct implementation utilizing delta, this difference should not matter.
<hr>

```
public MicrobeMovement()
{
    InputManager.AddInstance(this);
}

[RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
[RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
public void OnMovement(float delta, float value) {}
```
This example would theoretically work, but would not make sense, because you cannot differenciate between forward/backward input and left/right input.<br>
The correct way of implementing this would be this:
```
public MicrobeMovement()
{
    InputManager.AddInstance(this);
}
[RunOnAxis(new[] { "g_move_forward", "g_move_backwards" }, new[] { -1.0f, 1.0f })]
[RunOnAxis(new[] { "g_move_left", "g_move_right" }, new[] { -1.0f, 1.0f })]
[RunOnAxisGroup]
public void OnMovement(float delta, float forwardBackwardMovement, float leftRightMovement) {}
```
Using `RunOnAxisGroup` you can differenciate between forward/backward input and left/right input.

