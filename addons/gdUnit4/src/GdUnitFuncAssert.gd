## An Assertion Tool to verify function callback values
class_name GdUnitFuncAssert
extends GdUnitAssert


## Verifies that the current value is null.
func is_null() -> GdUnitFuncAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies that the current value is not null.
func is_not_null() -> GdUnitFuncAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies that the current value is equal to the given one.
@warning_ignore("unused_parameter")
func is_equal(expected :Variant) -> GdUnitFuncAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies that the current value is not equal to the given one.
@warning_ignore("unused_parameter")
func is_not_equal(expected :Variant) -> GdUnitFuncAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies that the current value is true.
func is_true() -> GdUnitFuncAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies that the current value is false.
func is_false() -> GdUnitFuncAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Overrides the default failure message by given custom message.
@warning_ignore("unused_parameter")
func override_failure_message(message :String) -> GdUnitFuncAssert:
	return self


## Sets the timeout in ms to wait the function returnd the expected value, if the time over a failure is emitted.[br]
## e.g.[br]
## do wait until 5s the function `is_state` is returns 10 [br]
## [code]assert_func(instance, "is_state").wait_until(5000).is_equal(10)[/code]
@warning_ignore("unused_parameter")
func wait_until(timeout :int) -> GdUnitFuncAssert:
	return self
