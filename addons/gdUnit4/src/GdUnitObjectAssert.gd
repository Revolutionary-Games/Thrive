## An Assertion Tool to verify Object values
class_name GdUnitObjectAssert
extends GdUnitAssert


## Verifies that the current object is equal to expected one.
@warning_ignore("unused_parameter")
func is_equal(expected: Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current object is not equal to expected one.
@warning_ignore("unused_parameter")
func is_not_equal(expected: Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current object is null.
func is_null() -> GdUnitObjectAssert:
	return self


## Verifies that the current object is not null.
func is_not_null() -> GdUnitObjectAssert:
	return self


## Verifies that the current object is the same as the given one.
@warning_ignore("unused_parameter", "shadowed_global_identifier")
func is_same(expected: Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current object is not the same as the given one.
@warning_ignore("unused_parameter")
func is_not_same(expected: Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current object is an instance of the given type.
@warning_ignore("unused_parameter")
func is_instanceof(type: Variant) -> GdUnitObjectAssert:
	return self


## Verifies that the current object is not an instance of the given type.
@warning_ignore("unused_parameter")
func is_not_instanceof(type: Variant) -> GdUnitObjectAssert:
	return self


## Checks whether the current object inherits from the specified type.
@warning_ignore("unused_parameter")
func is_inheriting(type: Variant) -> GdUnitObjectAssert:
	return self


## Checks whether the current object does NOT inherit from the specified type.
@warning_ignore("unused_parameter")
func is_not_inheriting(type: Variant) -> GdUnitObjectAssert:
	return self
