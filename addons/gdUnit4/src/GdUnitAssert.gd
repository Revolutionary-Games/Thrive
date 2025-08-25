## Base interface of all GdUnit asserts
class_name GdUnitAssert
extends RefCounted


## Verifies that the current value is null.
@warning_ignore("untyped_declaration")
func is_null():
	return self


## Verifies that the current value is not null.
@warning_ignore("untyped_declaration")
func is_not_null():
	return self


## Verifies that the current value is equal to expected one.
@warning_ignore("unused_parameter")
@warning_ignore("untyped_declaration")
func is_equal(expected: Variant):
	return self


## Verifies that the current value is not equal to expected one.
@warning_ignore("unused_parameter")
@warning_ignore("untyped_declaration")
func is_not_equal(expected: Variant):
	return self


## Overrides the default failure message by given custom message.[br]
## This function allows you to replace the automatically generated failure message with a more specific
## or user-friendly message that better describes the test failure context.[br]
## Usage:
##     [codeblock]
##		# Override with custom context-specific message
##		func test_player_inventory():
##		    assert_int(player.get_item_count("sword"))\
##		        .override_failure_message("Player should have exactly one sword")\
##		        .is_equal(1)
##     [/codeblock]
@warning_ignore("untyped_declaration")
func override_failure_message(_message: String):
	return self


## Appends a custom message to the failure message.[br]
## This can be used to add additional information to the generated failure message
## while keeping the original assertion details for better debugging context.[br]
## Usage:
##     [codeblock]
##		# Add context to existing failure message
##		func test_player_health():
##		    assert_int(player.health)\
##		        .append_failure_message("Player was damaged by: %s" % last_damage_source)\
##		        .is_greater(0)
##     [/codeblock]
@warning_ignore("untyped_declaration")
func append_failure_message(_message: String):
	return self
