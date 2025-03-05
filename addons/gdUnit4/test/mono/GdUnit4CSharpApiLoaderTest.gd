# GdUnit generated TestSuite
#warning-ignore-all:unused_argument
#warning-ignore-all:return_value_discarded
class_name GdUnit4CSharpApiLoaderTest
extends GdUnitTestSuite

# TestSuite generated from
const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")
const __source = 'res://addons/gdUnit4/src/mono/GdUnit4CSharpApiLoader.gd'


@warning_ignore("unused_parameter")
func before(do_skip := not GdUnit4CSharpApiLoader.is_mono_supported(), skip_reason := "Do run only for Godot Mono version") -> void:
	pass


@warning_ignore("unused_parameter")
func test_is_engine_version_supported(version :int, expected :bool, test_parameters := [
	[0x40000, false],
	[0x40001, false],
	[0x40002, false],
	[0x40100, false],
	[0x40101, false],
	[0x40102, false],
	[0x40100, false],
	[0x40200, true],
	[0x40201, true]]) -> void:

	assert_that(GdUnit4CSharpApiLoader.is_engine_version_supported(version)).is_equal(expected)


func test_api_version() -> void:
	assert_str(GdUnit4CSharpApiLoader.version()).starts_with("4.3")


func test_create_test_suite() -> void:
	var temp := create_temp_dir("examples")
	var result := GdUnitFileAccess.copy_file("res://addons/gdUnit4/test/resources/core/sources/TestPerson.cs", temp)
	assert_result(result).is_success()

	var example_source_cs := result.value_as_string()
	var source := load(example_source_cs)
	var test_suite_path := GdUnitTestSuiteScanner.resolve_test_suite_path(source.resource_path, "test")
	result = GdUnit4CSharpApiLoader.create_test_suite(source.resource_path, 18, test_suite_path)

	assert_result(result).is_success()
	var info: Dictionary = result.value()
	assert_str(info.get("path")).is_equal("user://tmp/test/examples/TestPersonTest.cs")
	assert_int(info.get("line")).is_equal(16)


func test_parse_test_suite() -> void:
	var test_suite := GdUnit4CSharpApiLoader.parse_test_suite("res://addons/gdUnit4/test/mono/GdUnit4CSharpApiTest.cs")
	assert_that(test_suite).is_not_null()
	assert_bool(test_suite.get("IsCsTestSuite")).is_true()
	test_suite.free()


class TestRunListener extends Node:
	pass


func test_executor() -> void:
	var listener :TestRunListener = auto_free(TestRunListener.new())
	var executor := GdUnit4CSharpApiLoader.create_executor(listener)
	assert_that(executor).is_not_null()

	var test_suite := GdUnit4CSharpApiLoader.parse_test_suite("res://addons/gdUnit4/test/mono/GdUnit4CSharpApiTest.cs")
	@warning_ignore("unsafe_method_access")
	assert_bool(executor.IsExecutable(test_suite)).is_true()

	test_suite.free()
