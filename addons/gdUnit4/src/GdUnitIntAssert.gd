## An Assertion Tool to verify integer values
class_name GdUnitIntAssert
extends GdUnitAssert

## Verifies that the current String is equal to the given one.
@warning_ignore("unused_parameter")
func is_equal(expected :Variant) -> GdUnitIntAssert:
	return self


## Verifies that the current String is not equal to the given one.
@warning_ignore("unused_parameter")
func is_not_equal(expected :Variant) -> GdUnitIntAssert:
	return self


## Verifies that the current value is less than the given one.
@warning_ignore("unused_parameter")
func is_less(expected :int) -> GdUnitIntAssert:
	return self


## Verifies that the current value is less than or equal the given one.
@warning_ignore("unused_parameter")
func is_less_equal(expected :int) -> GdUnitIntAssert:
	return self


## Verifies that the current value is greater than the given one.
@warning_ignore("unused_parameter")
func is_greater(expected :int) -> GdUnitIntAssert:
	return self


## Verifies that the current value is greater than or equal the given one.
@warning_ignore("unused_parameter")
func is_greater_equal(expected :int) -> GdUnitIntAssert:
	return self


## Verifies that the current value is even.
func is_even() -> GdUnitIntAssert:
	return self


## Verifies that the current value is odd.
func is_odd() -> GdUnitIntAssert:
	return self


## Verifies that the current value is negative.
func is_negative() -> GdUnitIntAssert:
	return self


## Verifies that the current value is not negative.
func is_not_negative() -> GdUnitIntAssert:
	return self


## Verifies that the current value is equal to zero.
func is_zero() -> GdUnitIntAssert:
	return self


## Verifies that the current value is not equal to zero.
func is_not_zero() -> GdUnitIntAssert:
	return self


## Verifies that the current value is in the given set of values.
@warning_ignore("unused_parameter")
func is_in(expected :Array) -> GdUnitIntAssert:
	return self


## Verifies that the current value is not in the given set of values.
@warning_ignore("unused_parameter")
func is_not_in(expected :Array) -> GdUnitIntAssert:
	return self


## Verifies that the current value is between the given boundaries (inclusive).
@warning_ignore("unused_parameter")
func is_between(from :int, to :int) -> GdUnitIntAssert:
	return self
