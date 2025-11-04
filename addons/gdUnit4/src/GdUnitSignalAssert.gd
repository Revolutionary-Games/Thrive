## An Assertion Tool to verify for emitted signals until a waiting time
@abstract class_name GdUnitSignalAssert
extends GdUnitAssert


## Verifies that the current value is null.
@abstract func is_null() -> GdUnitSignalAssert


## Verifies that the current value is not null.
@abstract func is_not_null() -> GdUnitSignalAssert


## Verifies that the current value is equal to the given one.
@abstract func is_equal(expected: Variant) -> GdUnitSignalAssert


## Verifies that the current value is not equal to expected one.
@abstract func is_not_equal(expected: Variant) -> GdUnitSignalAssert


## Overrides the default failure message by given custom message.
@abstract func override_failure_message(message: String) -> GdUnitSignalAssert


## Appends a custom message to the failure message.
@abstract func append_failure_message(message: String) -> GdUnitSignalAssert


## Verifies that given signal is emitted until waiting time
@abstract func is_emitted(name: String, args := []) -> GdUnitSignalAssert


## Verifies that given signal is NOT emitted until waiting time
@abstract func is_not_emitted(name: String, args := []) -> GdUnitSignalAssert


## Verifies the signal exists checked the emitter
@abstract func is_signal_exists(name: String) -> GdUnitSignalAssert


## Sets the assert signal timeout in ms, if the time over a failure is reported.[br]
## e.g.[br]
## do wait until 5s the instance has emitted the signal `signal_a`[br]
## [code]assert_signal(instance).wait_until(5000).is_emitted("signal_a")[/code]
@abstract func wait_until(timeout: int) -> GdUnitSignalAssert
