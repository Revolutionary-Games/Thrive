## An Assertion Tool to verify boolean values
class_name GdUnitBoolAssert
extends GdUnitAssert


## Verifies that the current value is null.
func is_null() -> GdUnitBoolAssert:
	return self


## Verifies that the current value is not null.
func is_not_null() -> GdUnitBoolAssert:
	return self


## Verifies that the current value is equal to the given one.
@warning_ignore("unused_parameter")
func is_equal(expected :Variant) -> GdUnitBoolAssert:
	return self


## Verifies that the current value is not equal to the given one.
@warning_ignore("unused_parameter")
func is_not_equal(expected :Variant) -> GdUnitBoolAssert:
	return self


## Verifies that the current value is true.
func is_true() -> GdUnitBoolAssert:
	return self


## Verifies that the current value is false.
func is_false() -> GdUnitBoolAssert:
	return self


## Overrides the default failure message by given custom message.
@warning_ignore("unused_parameter")
func override_failure_message(message :String) -> GdUnitBoolAssert:
	return self
