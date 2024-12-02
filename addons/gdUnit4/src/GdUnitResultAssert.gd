## An Assertion Tool to verify Results
class_name GdUnitResultAssert
extends GdUnitAssert


## Verifies that the current value is null.
func is_null() -> GdUnitResultAssert:
	return self


## Verifies that the current value is not null.
func is_not_null() -> GdUnitResultAssert:
	return self


## Verifies that the result is ends up with empty
func is_empty() -> GdUnitResultAssert:
	return self


## Verifies that the result is ends up with success
func is_success() -> GdUnitResultAssert:
	return self


## Verifies that the result is ends up with warning
func is_warning() -> GdUnitResultAssert:
	return self


## Verifies that the result is ends up with error
func is_error() -> GdUnitResultAssert:
	return self


## Verifies that the result contains the given message
@warning_ignore("unused_parameter")
func contains_message(expected :String) -> GdUnitResultAssert:
	return self


## Verifies that the result contains the given value
@warning_ignore("unused_parameter")
func is_value(expected :Variant) -> GdUnitResultAssert:
	return self
