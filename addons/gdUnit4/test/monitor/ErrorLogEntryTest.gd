# GdUnit generated TestSuite
class_name ErrorLogEntryTest
extends GdUnitTestSuite
@warning_ignore('unused_parameter')
@warning_ignore('return_value_discarded')

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/monitor/ErrorLogEntry.gd'

const error_report = """
	USER ERROR: this is an error
	   at: push_error (core/variant/variant_utility.cpp:880)
	"""
const script_error = """
	USER SCRIPT ERROR: Trying to call a function on a previously freed instance.
	   at: GdUnitScriptTypeTest.test_xx (res://addons/gdUnit4/test/GdUnitScriptTypeTest.gd:22)
"""


func test_parse_script_error_line_number() -> void:
	var line := ErrorLogEntry._parse_error_line_number(script_error.dedent())
	assert_int(line).is_equal(22)


func test_parse_push_error_line_number() -> void:
	var line := ErrorLogEntry._parse_error_line_number(error_report.dedent())
	assert_int(line).is_equal(-1)
