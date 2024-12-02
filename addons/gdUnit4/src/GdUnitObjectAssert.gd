## An Assertion Tool to verify Object values
class_name GdUnitObjectAssert
extends GdUnitAssert


## Verifies that the current value is equal to expected one.
@warning_ignore("unused_parameter")
func is_equal(expected :Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current value is not equal to expected one.
@warning_ignore("unused_parameter")
func is_not_equal(expected :Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current value is null.
func is_null() -> GdUnitObjectAssert:
	return self


## Verifies that the current value is not null.
func is_not_null() -> GdUnitObjectAssert:
	return self


## Verifies that the current value is the same as the given one.
@warning_ignore("unused_parameter", "shadowed_global_identifier")
func is_same(expected :Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current value is not the same as the given one.
@warning_ignore("unused_parameter")
func is_not_same(expected :Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current value is an instance of the given type.
@warning_ignore("unused_parameter")
func is_instanceof(expected :Object) -> GdUnitObjectAssert:
	return self


## Verifies that the current value is not an instance of the given type.
@warning_ignore("unused_parameter")
func is_not_instanceof(expected :Variant) -> GdUnitObjectAssert:
	return self
