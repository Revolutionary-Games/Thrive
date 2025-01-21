## An Assertion Tool to verify String values
class_name GdUnitStringAssert
extends GdUnitAssert


## Verifies that the current String is equal to the given one.
@warning_ignore("unused_parameter")
func is_equal(expected :Variant) -> GdUnitStringAssert:
	return self


## Verifies that the current String is equal to the given one, ignoring case considerations.
@warning_ignore("unused_parameter")
func is_equal_ignoring_case(expected :Variant) -> GdUnitStringAssert:
	return self


## Verifies that the current String is not equal to the given one.
@warning_ignore("unused_parameter")
func is_not_equal(expected :Variant) -> GdUnitStringAssert:
	return self


## Verifies that the current String is not equal to the given one, ignoring case considerations.
@warning_ignore("unused_parameter")
func is_not_equal_ignoring_case(expected :Variant) -> GdUnitStringAssert:
	return self


## Verifies that the current String is empty, it has a length of 0.
func is_empty() -> GdUnitStringAssert:
	return self


## Verifies that the current String is not empty, it has a length of minimum 1.
func is_not_empty() -> GdUnitStringAssert:
	return self


## Verifies that the current String contains the given String.
@warning_ignore("unused_parameter")
func contains(expected: String) -> GdUnitStringAssert:
	return self


## Verifies that the current String does not contain the given String.
@warning_ignore("unused_parameter")
func not_contains(expected: String) -> GdUnitStringAssert:
	return self


## Verifies that the current String does not contain the given String, ignoring case considerations.
@warning_ignore("unused_parameter")
func contains_ignoring_case(expected: String) -> GdUnitStringAssert:
	return self


## Verifies that the current String does not contain the given String, ignoring case considerations.
@warning_ignore("unused_parameter")
func not_contains_ignoring_case(expected: String) -> GdUnitStringAssert:
	return self


## Verifies that the current String starts with the given prefix.
@warning_ignore("unused_parameter")
func starts_with(expected: String) -> GdUnitStringAssert:
	return self


## Verifies that the current String ends with the given suffix.
@warning_ignore("unused_parameter")
func ends_with(expected: String) -> GdUnitStringAssert:
	return self


## Verifies that the current String has the expected length by used comparator.
@warning_ignore("unused_parameter")
func has_length(length: int, comparator: int = Comparator.EQUAL) -> GdUnitStringAssert:
	return self
