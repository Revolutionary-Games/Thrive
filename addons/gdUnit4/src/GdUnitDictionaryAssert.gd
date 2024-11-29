## An Assertion Tool to verify dictionary
class_name GdUnitDictionaryAssert
extends GdUnitAssert


## Verifies that the current value is null.
func is_null() -> GdUnitDictionaryAssert:
	return self


## Verifies that the current value is not null.
func is_not_null() -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary is equal to the given one, ignoring order.
@warning_ignore("unused_parameter")
func is_equal(expected :Variant) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary is not equal to the given one, ignoring order.
@warning_ignore("unused_parameter")
func is_not_equal(expected :Variant) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary is empty, it has a size of 0.
func is_empty() -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary is not empty, it has a size of minimum 1.
func is_not_empty() -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary is the same. [br]
## Compares the current by object reference equals
@warning_ignore("unused_parameter", "shadowed_global_identifier")
func is_same(expected :Variant) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary is NOT the same. [br]
## Compares the current by object reference equals
@warning_ignore("unused_parameter")
func is_not_same(expected :Variant) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary has a size of given value.
@warning_ignore("unused_parameter")
func has_size(expected: int) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary contains the given key(s).[br]
## The keys are compared by deep parameter comparision, for object reference compare you have to use [method contains_same_keys]
@warning_ignore("unused_parameter")
func contains_keys(expected :Array) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary contains the given key and value.[br]
## The key and value are compared by deep parameter comparision, for object reference compare you have to use [method contains_same_key_value]
@warning_ignore("unused_parameter")
func contains_key_value(key :Variant, value :Variant) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary not contains the given key(s).[br]
## This function is [b]deprecated[/b] you have to use [method not_contains_keys] instead
@warning_ignore("unused_parameter")
func contains_not_keys(expected :Array) -> GdUnitDictionaryAssert:
	push_warning("Deprecated: 'contains_not_keys' is deprectated and will be removed soon, use `not_contains_keys` instead!")
	return not_contains_keys(expected)


## Verifies that the current dictionary not contains the given key(s).[br]
## The keys are compared by deep parameter comparision, for object reference compare you have to use [method not_contains_same_keys]
@warning_ignore("unused_parameter")
func not_contains_keys(expected :Array) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary contains the given key(s).[br]
## The keys are compared by object reference, for deep parameter comparision use [method contains_keys]
@warning_ignore("unused_parameter")
func contains_same_keys(expected :Array) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary contains the given key and value.[br]
## The key and value are compared by object reference, for deep parameter comparision use [method contains_key_value]
@warning_ignore("unused_parameter")
func contains_same_key_value(key :Variant, value :Variant) -> GdUnitDictionaryAssert:
	return self


## Verifies that the current dictionary not contains the given key(s).
## The keys are compared by object reference, for deep parameter comparision use [method not_contains_keys]
@warning_ignore("unused_parameter")
func not_contains_same_keys(expected :Array) -> GdUnitDictionaryAssert:
	return self
