# GdUnit generated TestSuite
extends GdUnitTestSuite

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/core/parse/GdUnitTestParameterSetResolver.gd'
const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")

var _test_param1 := 10
var _test_param2 := 20


func before() -> void:
	_test_param1 = 11


func test_before() -> void:
	_test_param2 = 22


@warning_ignore("unused_parameter")
func test_example_a(a: int, b: int, test_parameters := [[1, 2], [3,4]]) -> void:
	pass


@warning_ignore("unused_parameter")
func test_example_b(a: Vector2, b: Vector2, test_parameters := [
	[Vector2.ZERO, Vector2.ONE], [Vector2(1.1, 3.2), Vector2.DOWN]] ) -> void:
	pass


@warning_ignore("unused_parameter")
func test_example_c(a: Object, b: Object, test_parameters := [
	[Resource.new(), Resource.new()],
	[Resource.new(), null]
	] ) -> void:
	pass


@warning_ignore("unused_parameter")
func test_resolve_parameters_static(a: int, b: int, test_parameters := [
	[1, 10],
	[2, 20]
	]) -> void:
	pass


@warning_ignore("unused_parameter")
func test_resolve_parameters_at_runtime(a: int, b: int, test_parameters := [
	[1, _test_param1],
	[2, _test_param2],
	[3, 30]
	]) -> void:
	pass


@warning_ignore("unused_parameter")
func test_parameterized_with_comments(a: int, b :int, c :String, expected :int, test_parameters := [
	# before data set
	[1, 2, '3', 6], # after data set
	# between data sets
	[3, 4, '5', 11],
	[6, 7, 'string #ABCD', 21], # dataset with [comment] singn
	[6, 7, "string #ABCD", 21] # dataset with "comment" singn
	#eof
]) -> void:
	pass


func build_param(value: float) -> Vector3:
	return Vector3(value, value, value)


@warning_ignore("unused_parameter")
func test_example_d(a: Vector3, b: Vector3, test_parameters:=[
	[build_param(1), build_param(3)],
	[Vector3.BACK, Vector3.UP]
	] ) -> void:
	pass


class TestObj extends RefCounted:
	var _value: String

	func _init(value: String) -> void:
		_value = value

	func _to_string() -> String:
		return _value


@warning_ignore("unused_parameter")
func test_example_e(a: Object, b: Object, expected: String, test_parameters:=[
	[TestObj.new("abc"), TestObj.new("def"), "abcdef"]]) -> void:
	pass


# verify the used 'test_parameters' is completly resolved
func test_load_parameter_sets() -> void:
	assert_array(load_parameter_sets("test_example_a")) \
		.is_equal([[1, 2], [3, 4]])

	assert_array(load_parameter_sets("test_example_b")) \
		.is_equal([[Vector2.ZERO, Vector2.ONE], [Vector2(1.1, 3.2), Vector2.DOWN]])

	assert_array(load_parameter_sets("test_example_c")) \
		.is_equal([[Resource.new(), Resource.new()], [Resource.new(), null]])

	assert_array(load_parameter_sets("test_example_d")) \
		.is_equal([[Vector3(1, 1, 1), Vector3(3, 3, 3)], [Vector3.BACK, Vector3.UP]])

	assert_array(load_parameter_sets("test_example_e")) \
		.is_equal([[TestObj.new("abc"), TestObj.new("def"), "abcdef"]])


func test_load_parameter_sets_at_runtime() -> void:
	var params := load_parameter_sets("test_resolve_parameters_at_runtime")
	assert_that(params).is_not_null()
	# check the parameters resolved at runtime
	assert_array(params) \
		.is_equal([
			# the value `_test_param1` is changed from 10 to 11 on `before` stage
			[1, 11],
			# the value `_test_param2` is changed from 20 to 2 on `test_before` stage
			[2, 22],
			# the value is static initial `30`
			[3, 30]])


func test_load_parameter_with_comments() -> void:
	var params := load_parameter_sets("test_parameterized_with_comments")
	assert_that(params).is_not_null()
	# check the parameters resolved at runtime
	assert_array(params) \
		.is_equal([
			[1, 2, '3', 6],
			[3, 4, '5', 11],
			[6, 7, 'string #ABCD', 21],
			[6, 7, "string #ABCD", 21]])


func test_validate_test_parameter_set() -> void:
	var test_suite :GdUnitTestSuite = auto_free(GdUnitTestResourceLoader.load_test_suite("res://addons/gdUnit4/test/core/resources/testsuites/TestSuiteInvalidParameterizedTests.resource"))

	assert_is_not_skipped(test_suite, "test_no_parameters")
	assert_is_not_skipped(test_suite, "test_parameterized_success", 0)
	assert_is_not_skipped(test_suite, "test_parameterized_success", 1)
	assert_is_not_skipped(test_suite, "test_parameterized_success", 2)
	assert_is_not_skipped(test_suite, "test_parameterized_failed", 0)
	assert_is_not_skipped(test_suite, "test_parameterized_failed", 1)
	assert_is_not_skipped(test_suite, "test_parameterized_failed", 2)
	assert_is_skipped(test_suite, "test_parameterized_to_less_args", 0).is_equal(
		"""
			The test data set at index (0) does not match the expected test arguments:
				test function: func test...(a: int,b: int,expected: int)
				test input values: [1, 2, 3, 6]
		""".dedent()
	)
	assert_is_skipped(test_suite, "test_parameterized_to_less_args", 1).is_equal(
		"""
			The test data set at index (1) does not match the expected test arguments:
				test function: func test...(a: int,b: int,expected: int)
				test input values: [3, 4, 5, 11]
		""".dedent()
	)
	assert_is_skipped(test_suite, "test_parameterized_to_many_args", 0).is_equal(
		"""
			The test data set at index (0) does not match the expected test arguments:
				test function: func test...(a: int,b: int,c: int,d: int,expected: int)
				test input values: [1, 2, 3, 6]
		""".dedent()
	)
	assert_is_skipped(test_suite, "test_parameterized_to_less_args", 0).is_equal(
		"""
			The test data set at index (0) does not match the expected test arguments:
				test function: func test...(a: int,b: int,expected: int)
				test input values: [1, 2, 3, 6]
		""".dedent()
	)
	# test_parameterized_invalid_struct
	assert_is_not_skipped(test_suite, "test_parameterized_invalid_struct", 0)
	assert_is_skipped(test_suite, "test_parameterized_invalid_struct", 1).is_equal(
		"""
			The test data set at index (1) does not match the expected test arguments:
				test function: func test...(a: int,b: int,expected: int)
				test input values: ["foo"]
		""".dedent()
	)
	assert_is_not_skipped(test_suite, "test_parameterized_invalid_struct", 2)
	# test_parameterized_invalid_args
	assert_is_not_skipped(test_suite, "test_parameterized_invalid_args", 0)
	assert_is_skipped(test_suite, "test_parameterized_invalid_args", 1).is_equal(
		"""
			The test data value does not match the expected input type!
				input value: '4', <String>
				expected argument: b: int
		""".dedent()
	)
	assert_is_not_skipped(test_suite, "test_parameterized_invalid_args", 2)


func assert_is_not_skipped(test_suite: GdUnitTestSuite, test_case: String, index := -1) -> void:
	var test := GdUnitTools.find_test_case(test_suite, test_case, index)
	if test.is_parameterized():
		# to load parameter set and force validate
		test._resolve_test_parameters(index)
	assert_bool(test.is_skipped()).is_false()


func assert_is_skipped(test_suite: GdUnitTestSuite, test_case: String, index := -1) -> GdUnitStringAssert:
	var test := GdUnitTools.find_test_case(test_suite, test_case, index)
	if test.is_parameterized():
		# to load parameter set and force validate
		test._resolve_test_parameters(index)
	assert_bool(test.is_skipped()).is_true()
	return assert_str(GdUnitTools.richtext_normalize(test.skip_info()))


func load_parameter_sets(child_name: String) -> Array:
	var script: GDScript = self.get_script()
	var function_descriptors := GdScriptParser.new().get_function_descriptors(script, [child_name])
	var fd: GdFunctionDescriptor = function_descriptors.front()
	var result := GdUnitTestParameterSetResolver.new(fd).load_parameter_sets(self)
	if result.is_success():
		return result.value()
	return []
