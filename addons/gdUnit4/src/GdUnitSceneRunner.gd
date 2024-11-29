## The Scene Runner is a tool used for simulating interactions on a scene.
## With this tool, you can simulate input events such as keyboard or mouse input and/or simulate scene processing over a certain number of frames.
## This tool is typically used for integration testing a scene.
class_name GdUnitSceneRunner
extends RefCounted

const NO_ARG = GdUnitConstants.NO_ARG


## Simulates that an action has been pressed.[br]
## [member action] : the action e.g. [code]"ui_up"[/code][br]
@warning_ignore("unused_parameter")
func simulate_action_pressed(action: String) -> GdUnitSceneRunner:
	return self


## Simulates that an action is pressed.[br]
## [member action] : the action e.g. [code]"ui_up"[/code][br]
@warning_ignore("unused_parameter")
func simulate_action_press(action: String) -> GdUnitSceneRunner:
	return self


## Simulates that an action has been released.[br]
## [member action] : the action e.g. [code]"ui_up"[/code][br]
@warning_ignore("unused_parameter")
func simulate_action_release(action: String) -> GdUnitSceneRunner:
	return self


## Simulates that a key has been pressed.[br]
## [member key_code] : the key code e.g. [constant KEY_ENTER][br]
## [member shift_pressed] : false by default set to true if simmulate shift is press[br]
## [member ctrl_pressed] : false by default set to true if simmulate control is press[br]
## [codeblock]
##    func test_key_presssed():
##       var runner = scene_runner("res://scenes/simple_scene.tscn")
##       await runner.simulate_key_pressed(KEY_SPACE)
## [/codeblock]
@warning_ignore("unused_parameter")
func simulate_key_pressed(key_code: int, shift_pressed := false, ctrl_pressed := false) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates that a key is pressed.[br]
## [member key_code] : the key code e.g. [constant KEY_ENTER][br]
## [member shift_pressed] : false by default set to true if simmulate shift is press[br]
## [member ctrl_pressed] : false by default set to true if simmulate control is press[br]
@warning_ignore("unused_parameter")
func simulate_key_press(key_code: int, shift_pressed := false, ctrl_pressed := false) -> GdUnitSceneRunner:
	return self


## Simulates that a key has been released.[br]
## [member key_code] : the key code e.g. [constant KEY_ENTER][br]
## [member shift_pressed] : false by default set to true if simmulate shift is press[br]
## [member ctrl_pressed] : false by default set to true if simmulate control is press[br]
@warning_ignore("unused_parameter")
func simulate_key_release(key_code: int, shift_pressed := false, ctrl_pressed := false) -> GdUnitSceneRunner:
	return self


## Sets the mouse cursor to given position relative to the viewport.
## @deprecated: Use [set_mouse_position] instead.
@warning_ignore("unused_parameter")
func set_mouse_pos(position: Vector2) -> GdUnitSceneRunner:
	return self


## Sets the mouse position to the specified vector, provided in pixels and relative to an origin at the upper left corner of the currently focused Window Manager game window.[br]
## [member position] : The absolute position in pixels as Vector2
@warning_ignore("unused_parameter")
func set_mouse_position(position: Vector2) -> GdUnitSceneRunner:
	return self


## Returns the mouse's position in this Viewport using the coordinate system of this Viewport.
func get_mouse_position() -> Vector2:
	return Vector2.ZERO


## Gets the current global mouse position of the current window
func get_global_mouse_position() -> Vector2:
	return Vector2.ZERO


## Simulates a mouse moved to final position.[br]
## [member position] : The final mouse position
@warning_ignore("unused_parameter")
func simulate_mouse_move(position: Vector2) -> GdUnitSceneRunner:
	return self


## Simulates a mouse move to the relative coordinates (offset).[br]
## [color=yellow]You must use [b]await[/b] to wait until the simulated mouse movement is complete.[/color][br]
## [br]
## [member relative] : The relative position, indicating the mouse position offset.[br]
## [member time] : The time to move the mouse by the relative position in seconds (default is 1 second).[br]
## [member trans_type] : Sets the type of transition used (default is TRANS_LINEAR).[br]
## [codeblock]
##    func test_move_mouse():
##       var runner = scene_runner("res://scenes/simple_scene.tscn")
##       await runner.simulate_mouse_move_relative(Vector2(100,100))
## [/codeblock]
@warning_ignore("unused_parameter")
func simulate_mouse_move_relative(relative: Vector2, time: float = 1.0, trans_type: Tween.TransitionType = Tween.TRANS_LINEAR) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates a mouse move to the absolute coordinates.[br]
## [color=yellow]You must use [b]await[/b] to wait until the simulated mouse movement is complete.[/color][br]
## [br]
## [member position] : The final position of the mouse.[br]
## [member time] : The time to move the mouse to the final position in seconds (default is 1 second).[br]
## [member trans_type] : Sets the type of transition used (default is TRANS_LINEAR).[br]
## [codeblock]
##    func test_move_mouse():
##       var runner = scene_runner("res://scenes/simple_scene.tscn")
##       await runner.simulate_mouse_move_absolute(Vector2(100,100))
## [/codeblock]
@warning_ignore("unused_parameter")
func simulate_mouse_move_absolute(position: Vector2, time: float = 1.0, trans_type: Tween.TransitionType = Tween.TRANS_LINEAR) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates a mouse button pressed.[br]
## [member button_index] : The mouse button identifier, one of the [enum MouseButton] or button wheel constants.
## [member double_click] : Set to true to simulate a double-click
@warning_ignore("unused_parameter")
func simulate_mouse_button_pressed(button_index: MouseButton, double_click := false) -> GdUnitSceneRunner:
	return self


## Simulates a mouse button press (holding)[br]
## [member button_index] : The mouse button identifier, one of the [enum MouseButton] or button wheel constants.
## [member double_click] : Set to true to simulate a double-click
@warning_ignore("unused_parameter")
func simulate_mouse_button_press(button_index: MouseButton, double_click := false) -> GdUnitSceneRunner:
	return self


## Simulates a mouse button released.[br]
## [member button_index] : The mouse button identifier, one of the [enum MouseButton] or button wheel constants.
@warning_ignore("unused_parameter")
func simulate_mouse_button_release(button_index: MouseButton) -> GdUnitSceneRunner:
	return self


## Simulates a screen touch is pressed.[br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member position] : The position to touch the screen.[br]
## [member double_tap] : If true, the touch's state is a double tab.
@warning_ignore("unused_parameter")
func simulate_screen_touch_pressed(index: int, position: Vector2, double_tap := false) -> GdUnitSceneRunner:
	return self


## Simulates a screen touch press without releasing it immediately, effectively simulating a "hold" action.[br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member position] : The position to touch the screen.[br]
## [member double_tap] : If true, the touch's state is a double tab.
@warning_ignore("unused_parameter")
func simulate_screen_touch_press(index: int, position: Vector2, double_tap := false) -> GdUnitSceneRunner:
	return self


## Simulates a screen touch is released.[br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member double_tap] : If true, the touch's state is a double tab.
@warning_ignore("unused_parameter")
func simulate_screen_touch_release(index: int, double_tap := false) -> GdUnitSceneRunner:
	return self


## Simulates a touch drag and drop event to a relative position.[br]
## [color=yellow]You must use [b]await[/b] to wait until the simulated drag&drop is complete.[/color][br]
## [br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member relative] : The relative position, indicating the drag&drop position offset.[br]
## [member time] : The time to move to the relative position in seconds (default is 1 second).[br]
## [member trans_type] : Sets the type of transition used (default is TRANS_LINEAR).[br]
## [codeblock]
##    func test_touch_drag_drop():
##       var runner = scene_runner("res://scenes/simple_scene.tscn")
##       # start drag at position 50,50
##       runner.simulate_screen_touch_drag_begin(1, Vector2(50, 50))
##       # and drop it at final at 150,50  relative (50,50 + 100,0)
##       await runner.simulate_screen_touch_drag_relative(1, Vector2(100,0))
## [/codeblock]
@warning_ignore("unused_parameter")
func simulate_screen_touch_drag_relative(index: int, relative: Vector2, time: float = 1.0, trans_type: Tween.TransitionType = Tween.TRANS_LINEAR) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates a touch screen drop to the absolute coordinates (offset).[br]
## [color=yellow]You must use [b]await[/b] to wait until the simulated drop is complete.[/color][br]
## [br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member position] : The final position, indicating the drop position.[br]
## [member time] : The time to move to the final position in seconds (default is 1 second).[br]
## [member trans_type] : Sets the type of transition used (default is TRANS_LINEAR).[br]
## [codeblock]
##    func test_touch_drag_drop():
##       var runner = scene_runner("res://scenes/simple_scene.tscn")
##       # start drag at position 50,50
##       runner.simulate_screen_touch_drag_begin(1, Vector2(50, 50))
##       # and drop it at 100,50
##       await runner.simulate_screen_touch_drag_absolute(1, Vector2(100,50))
## [/codeblock]
@warning_ignore("unused_parameter")
func simulate_screen_touch_drag_absolute(index: int, position: Vector2, time: float = 1.0, trans_type: Tween.TransitionType = Tween.TRANS_LINEAR) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates a complete drag and drop event from one position to another.[br]
## This is ideal for testing complex drag-and-drop scenarios that require a specific start and end position.[br]
## [color=yellow]You must use [b]await[/b] to wait until the simulated drop is complete.[/color][br]
## [br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member position] : The drag start position, indicating the drag position.[br]
## [member drop_position] : The drop position, indicating the drop position.[br]
## [member time] : The time to move to the final position in seconds (default is 1 second).[br]
## [member trans_type] : Sets the type of transition used (default is TRANS_LINEAR).[br]
## [codeblock]
##    func test_touch_drag_drop():
##       var runner = scene_runner("res://scenes/simple_scene.tscn")
##       # start drag at position 50,50 and drop it at 100,50
##       await runner.simulate_screen_touch_drag_drop(1, Vector2(50, 50), Vector2(100,50))
## [/codeblock]
@warning_ignore("unused_parameter")
func simulate_screen_touch_drag_drop(index: int, position: Vector2, drop_position: Vector2, time: float = 1.0, trans_type: Tween.TransitionType = Tween.TRANS_LINEAR) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates a touch screen drag event to given position.[br]
## [member index] : The touch index in the case of a multi-touch event.[br]
## [member position] : The drag start position, indicating the drag position.[br]
@warning_ignore("unused_parameter")
func simulate_screen_touch_drag(index: int, position: Vector2) -> GdUnitSceneRunner:
	return self


## Returns the actual position of the touchscreen drag position by given index.
## [member index] : The touch index in the case of a multi-touch event.[br]
@warning_ignore("unused_parameter")
func get_screen_touch_drag_position(index: int) -> Vector2:
	return Vector2.ZERO


## Sets how fast or slow the scene simulation is processed (clock ticks versus the real).[br]
## It defaults to 1.0. A value of 2.0 means the game moves twice as fast as real life,
## whilst a value of 0.5 means the game moves at half the regular speed.


## Sets the time factor for the scene simulation.
## [member time_factor] : A float representing the simulation speed.[br]
## - Default is 1.0, meaning the simulation runs at normal speed.[br]
## - A value of 2.0 means the simulation runs twice as fast as real time.[br]
## - A value of 0.5 means the simulation runs at half the regular speed.[br]
@warning_ignore("unused_parameter")
func set_time_factor(time_factor: float = 1.0) -> GdUnitSceneRunner:
	return self


## Simulates scene processing for a certain number of frames.[br]
## [member frames] : amount of frames to process[br]
## [member delta_milli] : the time delta between a frame in milliseconds
@warning_ignore("unused_parameter")
func simulate_frames(frames: int, delta_milli: int = -1) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates scene processing until the given signal is emitted by the scene.[br]
## [member signal_name] : the signal to stop the simulation[br]
## [member args] : optional signal arguments to be matched for stop[br]
@warning_ignore("unused_parameter")
func simulate_until_signal(
	signal_name: String,
	arg0: Variant = NO_ARG,
	arg1: Variant = NO_ARG,
	arg2: Variant = NO_ARG,
	arg3: Variant = NO_ARG,
	arg4: Variant = NO_ARG,
	arg5: Variant = NO_ARG,
	arg6: Variant = NO_ARG,
	arg7: Variant = NO_ARG,
	arg8: Variant = NO_ARG,
	arg9: Variant = NO_ARG) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Simulates scene processing until the given signal is emitted by the given object.[br]
## [member source] : the object that should emit the signal[br]
## [member signal_name] : the signal to stop the simulation[br]
## [member args] : optional signal arguments to be matched for stop
@warning_ignore("unused_parameter")
func simulate_until_object_signal(
	source: Object,
	signal_name: String,
	arg0: Variant = NO_ARG,
	arg1: Variant = NO_ARG,
	arg2: Variant = NO_ARG,
	arg3: Variant = NO_ARG,
	arg4: Variant = NO_ARG,
	arg5: Variant = NO_ARG,
	arg6: Variant = NO_ARG,
	arg7: Variant = NO_ARG,
	arg8: Variant = NO_ARG,
	arg9: Variant = NO_ARG) -> GdUnitSceneRunner:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Waits for all input events to be processed by flushing any buffered input events
## and then awaiting a full cycle of both the process and physics frames.[br]
## [br]
## This is typically used to ensure that any simulated or queued inputs are fully
## processed before proceeding with the next steps in the scene.[br]
## It's essential for reliable input simulation or when synchronizing logic based
## on inputs.[br]
##
## Usage Example:
## [codeblock]
## 	await await_input_processed()  # Ensure all inputs are processed before continuing
## [/codeblock]
func await_input_processed() -> void:
	if scene() != null and scene().process_mode != Node.PROCESS_MODE_DISABLED:
		Input.flush_buffered_events()
	await (Engine.get_main_loop() as SceneTree).process_frame
	await (Engine.get_main_loop() as SceneTree).physics_frame


## The await_func function pauses execution until a specified function in the scene returns a value.[br]
## It returns a [GdUnitFuncAssert], which provides a suite of assertion methods to verify the returned value.[br]
## [member func_name] : The name of the function to wait for.[br]
## [member args] : Optional function arguments
## [br]
## Usage Example:
## [codeblock]
## 	# Waits for 'calculate_score' function and verifies the result is equal to 100.
## 	await_func("calculate_score").is_equal(100)
## [/codeblock]
@warning_ignore("unused_parameter")
func await_func(func_name: String, args := []) -> GdUnitFuncAssert:
	return null



## The await_func_on function extends the functionality of await_func by allowing you to specify a source node within the scene.[br]
## It waits for a specified function on that node to return a value and returns a [GdUnitFuncAssert] object for assertions.[br]
## [member source] : The object where implements the function.[br]
## [member func_name] : The name of the function to wait for.[br]
## [member args] : optional function arguments
## [br]
## Usage Example:
## [codeblock]
## 	# Waits for 'calculate_score' function and verifies the result is equal to 100.
## 	var my_instance := ScoreCalculator.new()
## 	await_func(my_instance, "calculate_score").is_equal(100)
## [/codeblock]
@warning_ignore("unused_parameter")
func await_func_on(source: Object, func_name: String, args := []) -> GdUnitFuncAssert:
	return null


## Waits for the specified signal to be emitted by the scene. If the signal is not emitted within the given timeout, the operation fails.[br]
## [member signal_name] : The name of the signal to wait for[br]
## [member args] : The signal arguments as an array[br]
## [member timeout] : The maximum duration (in milliseconds) to wait for the signal to be emitted before failing
@warning_ignore("unused_parameter")
func await_signal(signal_name: String, args := [], timeout := 2000 ) -> void:
	await (Engine.get_main_loop() as SceneTree).process_frame
	pass


## Waits for the specified signal to be emitted by a particular source node. If the signal is not emitted within the given timeout, the operation fails.[br]
## [member source] : the object from which the signal is emitted[br]
## [member signal_name] : The name of the signal to wait for[br]
## [member args] : The signal arguments as an array[br]
## [member timeout] : tThe maximum duration (in milliseconds) to wait for the signal to be emitted before failing
@warning_ignore("unused_parameter")
func await_signal_on(source: Object, signal_name: String, args := [], timeout := 2000 ) -> void:
	pass


## Restores the scene window to a windowed mode and brings it to the foreground.[br]
## This ensures that the scene is visible and active during testing, making it easier to observe and interact with.
func move_window_to_foreground() -> GdUnitSceneRunner:
	return self


## Restores the scene window to a windowed mode and brings it to the foreground.[br]
## This ensures that the scene is visible and active during testing, making it easier to observe and interact with.
## @deprecated: Use [move_window_to_foreground] instead.
func maximize_view() -> GdUnitSceneRunner:
	return self


## Return the current value of the property with the name <name>.[br]
## [member name] : name of property[br]
## [member return] : the value of the property
@warning_ignore("unused_parameter")
func get_property(name: String) -> Variant:
	return null

## Set the  value <value> of the property with the name <name>.[br]
## [member name] : name of property[br]
## [member value] : value of property[br]
## [member return] : true|false depending on valid property name.
@warning_ignore("unused_parameter")
func set_property(name: String, value: Variant) -> bool:
	return false


## executes the function specified by <name> in the scene and returns the result.[br]
## [member name] : the name of the function to execute[br]
## [member args] : optional function arguments[br]
## [member return] : the function result
@warning_ignore("unused_parameter")
func invoke(
	name: String,
	arg0: Variant = NO_ARG,
	arg1: Variant = NO_ARG,
	arg2: Variant = NO_ARG,
	arg3: Variant = NO_ARG,
	arg4: Variant = NO_ARG,
	arg5: Variant = NO_ARG,
	arg6: Variant = NO_ARG,
	arg7: Variant = NO_ARG,
	arg8: Variant = NO_ARG,
	arg9: Variant = NO_ARG) -> Variant:
	return null


## Searches for the specified node with the name in the current scene and returns it, otherwise null.[br]
## [member name] : the name of the node to find[br]
## [member recursive] : enables/disables seraching recursive[br]
## [member return] : the node if find otherwise null
@warning_ignore("unused_parameter")
func find_child(name: String, recursive: bool = true, owned: bool = false) -> Node:
	return null


## Access to current running scene
func scene() -> Node:
	return null
