## An Assertion Tool to verify for emitted signals until a waiting time
class_name GdUnitSignalAssert
extends GdUnitAssert


## Verifies that given signal is emitted until waiting time
@warning_ignore("unused_parameter")
func is_emitted(name :String, args := []) -> GdUnitSignalAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies that given signal is NOT emitted until waiting time
@warning_ignore("unused_parameter")
func is_not_emitted(name :String, args := []) -> GdUnitSignalAssert:
	await (Engine.get_main_loop() as SceneTree).process_frame
	return self


## Verifies the signal exists checked the emitter
@warning_ignore("unused_parameter")
func is_signal_exists(name :String) -> GdUnitSignalAssert:
	return self


## Overrides the default failure message by given custom message.
@warning_ignore("unused_parameter")
func override_failure_message(message :String) -> GdUnitSignalAssert:
	return self


## Sets the assert signal timeout in ms, if the time over a failure is reported.[br]
## e.g.[br]
## do wait until 5s the instance has emitted the signal `signal_a`[br]
## [code]assert_signal(instance).wait_until(5000).is_emitted("signal_a")[/code]
@warning_ignore("unused_parameter")
func wait_until(timeout :int) -> GdUnitSignalAssert:
	return self
