## GdUnitTestCase
## A class representing a single test case in GdUnit4.
## This class is used as a data container to hold all relevant information about a test case,
## including its location, dependencies, and metadata for test discovery and execution.

class_name GdUnitTestCase
extends RefCounted

## A unique identifier for the test case. Used to track and reference specific test instances.
var guid := GdUnitGUID.new()

## The name of the test method/function. Should start with "test_" prefix.
var test_name: String

##  The class name of the test suite containing this test case.
var suite_name: String

## The fully qualified name of the test case following C# namespace pattern:
## Constructed from the folder path (where folders are dot-separated), the test suite name, and the test case name.
## All parts are joined by dots: {folder1.folder2.folder3}.{suite_name}.{test_name}
var fully_qualified_name: String

var display_name: String

## Index tracking test attributes for ordered execution. Default is 0.
## Higher values indicate later execution in the test sequence.
var attribute_index: int

## Flag indicating if this test requires the Godot runtime environment.
## Tests requiring runtime cannot be executed in isolation.
var require_godot_runtime: bool = true

## The path to the source file containing this test case.
## Used for test discovery and execution.
var source_file: String

## The line number where the test case is defined in the source file.
## Used for navigation and error reporting.
var line_number: int = -1

## Additional metadata about the test case, such as:
## - tags: Array[String] - Test categories/tags for filtering
## - timeout: int - Maximum execution time in milliseconds
## - skip: bool - Whether the test should be skipped
## - dependencies: Array[String] - Required test dependencies
var metadate: Dictionary = {}


static func from(_source_file: String, _line_number: int, _test_name: String, _attribute_index := -1, _test_parameters := "") -> GdUnitTestCase:
	if(_source_file == null or _source_file.is_empty()):
		prints(_test_name)

	assert(_test_name != null and not _test_name.is_empty(), "Precondition: The parameter 'test_name' is not set")
	assert(_source_file != null and not _source_file.is_empty(), "Precondition: The parameter 'source_file' is not set")

	var test := GdUnitTestCase.new()
	test.test_name = _test_name
	test.source_file = _source_file
	test.line_number = _line_number
	test.attribute_index = _attribute_index
	test._build_suite_name()
	test._build_display_name(_test_parameters)
	test._build_fully_qualified_name()
	return test


func _build_suite_name() -> void:
	suite_name = source_file.get_file().get_basename()
	assert(suite_name != null and not suite_name.is_empty(), "Precondition: The parameter 'suite_name' can't be resolved")


func _build_display_name(_test_parameters: String) -> void:
	if attribute_index == -1:
		display_name = test_name
	else:
		display_name = "%s:%d (%s)" % [test_name, attribute_index, _test_parameters.trim_prefix("[").trim_suffix("]").replace('"', "'")]


func _build_fully_qualified_name() -> void:
	var name_space := source_file.trim_prefix("res://").trim_suffix(".gd").trim_suffix(".cs").replace("/", ".")

	if attribute_index == -1:
		fully_qualified_name = "%s.%s" % [name_space, test_name]
	else:
		fully_qualified_name = "%s.%s.%s" % [name_space, test_name, display_name]
	assert(fully_qualified_name != null and not fully_qualified_name.is_empty(), "Precondition: The parameter 'fully_qualified_name' can't be resolved")
