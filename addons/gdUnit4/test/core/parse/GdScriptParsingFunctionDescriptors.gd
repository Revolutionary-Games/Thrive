extends GdUnitTestSuite

var _parser: GdScriptParser


func before() -> void:
	_parser = GdScriptParser.new()


func after() -> void:
	clean_temp_dir()


func test_default_args_dictionary() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/container_build_in_types/ClassWithDictionaryDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_dictionary_case1", script.resource_path, 5, TYPE_ARRAY, [
				GdFunctionArgument.new("dict", TYPE_DICTIONARY, {}),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_dictionary_case2", script.resource_path, 9, TYPE_ARRAY, [
				GdFunctionArgument.new("dict", TYPE_DICTIONARY, {}),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_dictionary_case3", script.resource_path, 13, TYPE_ARRAY, [
				GdFunctionArgument.new("dict", TYPE_DICTIONARY, {}),
			])
		)
	assert_that(fds[3])\
		.is_equal(GdFunctionDescriptor
			.create("on_dictionary_case4", script.resource_path, 17, TYPE_ARRAY, [
				GdFunctionArgument.new("dict", TYPE_DICTIONARY, {"a":"value_a", "b":"value_b"}),
			])
		)
	assert_that(fds[4])\
		.is_equal(GdFunctionDescriptor
			.create("on_dictionary_case5", script.resource_path, 21, TYPE_ARRAY, [
				GdFunctionArgument.new("dict", TYPE_DICTIONARY, {"a":"value_a", "b":"value_b"}),
			])
		)
	assert_that(fds[5])\
		.is_equal(GdFunctionDescriptor
			.create("on_dictionary_case6", script.resource_path, 27, TYPE_ARRAY, [
				GdFunctionArgument.new("dict", TYPE_DICTIONARY, {"a":"value_a", "b":"value_b"}),
			])
		)


func test_default_args_array() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/container_build_in_types/ClassWithArrayDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_array_case1", script.resource_path, 5, TYPE_ARRAY, [
				GdFunctionArgument.new("values", TYPE_ARRAY, []),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_array_case2", script.resource_path, 9, TYPE_ARRAY, [
				GdFunctionArgument.new("values", TYPE_ARRAY, []),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_array_case3", script.resource_path, 13, TYPE_ARRAY, [
				GdFunctionArgument.new("values", TYPE_ARRAY, []),
			])
		)
	assert_that(fds[3])\
		.is_equal(GdFunctionDescriptor
			.create("on_array_case4", script.resource_path, 17, TYPE_ARRAY, [
				GdFunctionArgument.new("values", TYPE_ARRAY, [1, "3", [], {}]),
			])
		)
	assert_that(fds[4])\
		.is_equal(GdFunctionDescriptor
			.create("on_array_case5", script.resource_path, 21, TYPE_ARRAY, [
				GdFunctionArgument.new("values", TYPE_ARRAY, [1, "3", [], {}]),
			])
		)
	assert_that(fds[5])\
		.is_equal(GdFunctionDescriptor
			.create("on_array_case6", script.resource_path, 29, TYPE_ARRAY, [
				GdFunctionArgument.new("values", TYPE_ARRAY, [1, "3", [], {}]),
			])
		)


func test_default_args_callable() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/container_build_in_types/ClassWithCallableDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_callable_case1", script.resource_path, 5, TYPE_CALLABLE, [
				GdFunctionArgument.new("cb", TYPE_CALLABLE),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_callable_case2", script.resource_path, 9, TYPE_CALLABLE, [
				GdFunctionArgument.new("cb", TYPE_CALLABLE, Callable()),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_callable_case3", script.resource_path, 13, TYPE_CALLABLE, [
				GdFunctionArgument.new("cb", TYPE_CALLABLE, Callable()),
			])
		)
	assert_that(fds[3])\
		.is_equal(GdFunctionDescriptor
			.create("on_callable_case4", script.resource_path, 17, TYPE_CALLABLE, [
				GdFunctionArgument.new("cb", TYPE_CALLABLE, Callable(null, "method_foo")),
			])
		)


# Basic build-in-types
func test_default_args_basic_type_int() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeIntDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_int_case1", script.resource_path, 5, TYPE_INT, [
				GdFunctionArgument.new("value", TYPE_INT),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_int_case2", script.resource_path, 9, TYPE_INT, [
				GdFunctionArgument.new("value", TYPE_INT, 42),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_int_case3", script.resource_path, 13, TYPE_INT, [
				GdFunctionArgument.new("value", TYPE_INT, 42),
			])
		)


func test_default_args_basic_type_float() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeFloatDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_float_case1", script.resource_path, 5, TYPE_FLOAT, [
				GdFunctionArgument.new("value", TYPE_FLOAT),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_float_case2", script.resource_path, 9, TYPE_FLOAT, [
				GdFunctionArgument.new("value", TYPE_FLOAT, 42.1),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_float_case3", script.resource_path, 13, TYPE_FLOAT, [
				GdFunctionArgument.new("value", TYPE_FLOAT, 42.1),
			])
		)


func test_default_args_basic_type_bool() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeBoolDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_bool_case1", script.resource_path, 5, TYPE_BOOL, [
				GdFunctionArgument.new("value", TYPE_BOOL),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_bool_case2", script.resource_path, 9, TYPE_BOOL, [
				GdFunctionArgument.new("value", TYPE_BOOL, true),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_bool_case3", script.resource_path, 13, TYPE_BOOL, [
				GdFunctionArgument.new("value", TYPE_BOOL, true),
			])
		)


func test_default_args_basic_type_string() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeStringDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_string_case1", script.resource_path, 5, TYPE_STRING, [
				GdFunctionArgument.new("value", TYPE_STRING),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_string_case2", script.resource_path, 9, TYPE_STRING, [
				GdFunctionArgument.new("value", TYPE_STRING, "foo"),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_string_case3", script.resource_path, 13, TYPE_STRING, [
				GdFunctionArgument.new("value", TYPE_STRING, "foo"),
			])
		)


func test_default_args_basic_type_string_name() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeStringNameDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_string_name_case1", script.resource_path, 5, TYPE_STRING_NAME, [
				GdFunctionArgument.new("value", TYPE_STRING_NAME),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_string_name_case2", script.resource_path, 9, TYPE_STRING_NAME, [
				GdFunctionArgument.new("value", TYPE_STRING_NAME, &"foo"),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_string_name_case3", script.resource_path, 13, TYPE_STRING_NAME, [
				GdFunctionArgument.new("value", TYPE_STRING_NAME, &"foo"),
			])
		)


func test_default_args_basic_type_node_path() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeNodePathDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_node_path_case1", script.resource_path, 5, TYPE_NODE_PATH, [
				GdFunctionArgument.new("value", TYPE_NODE_PATH),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_node_path_case2", script.resource_path, 9, TYPE_NODE_PATH, [
				GdFunctionArgument.new("value", TYPE_NODE_PATH, NodePath("foo1")),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_node_path_case3", script.resource_path, 13, TYPE_NODE_PATH, [
				GdFunctionArgument.new("value", TYPE_NODE_PATH, NodePath("foo2")),
			])
		)
	assert_that(fds[3])\
		.is_equal(GdFunctionDescriptor
			.create("on_node_path_case4", script.resource_path, 17, TYPE_NODE_PATH, [
				GdFunctionArgument.new("value", TYPE_NODE_PATH, NodePath("foo3")),
			])
		)


func test_default_args_basic_type_vector2() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeVector2DefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector2_case1", script.resource_path, 5, TYPE_VECTOR2, [
				GdFunctionArgument.new("value", TYPE_VECTOR2),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector2_case2", script.resource_path, 9, TYPE_VECTOR2, [
				GdFunctionArgument.new("value", TYPE_VECTOR2, Vector2.ONE),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector2_case3", script.resource_path, 13, TYPE_VECTOR2, [
				GdFunctionArgument.new("value", TYPE_VECTOR2, Vector2.ONE),
			])
		)


func test_default_args_basic_type_vector2i() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeVector2iDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector2i_case1", script.resource_path, 5, TYPE_VECTOR2I, [
				GdFunctionArgument.new("value", TYPE_VECTOR2I),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector2i_case2", script.resource_path, 9, TYPE_VECTOR2I, [
				GdFunctionArgument.new("value", TYPE_VECTOR2I, Vector2i.ONE),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector2i_case3", script.resource_path, 13, TYPE_VECTOR2I, [
				GdFunctionArgument.new("value", TYPE_VECTOR2I, Vector2i.ONE),
			])
		)


func test_default_args_basic_type_vector3() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeVector3DefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector3_case1", script.resource_path, 5, TYPE_VECTOR3, [
				GdFunctionArgument.new("value", TYPE_VECTOR3),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector3_case2", script.resource_path, 9, TYPE_VECTOR3, [
				GdFunctionArgument.new("value", TYPE_VECTOR3, Vector3.ONE),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector3_case3", script.resource_path, 13, TYPE_VECTOR3, [
				GdFunctionArgument.new("value", TYPE_VECTOR3, Vector3.ONE),
			])
		)


func test_default_args_basic_type_vector3i() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeVector3iDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector3i_case1", script.resource_path, 5, TYPE_VECTOR3I, [
				GdFunctionArgument.new("value", TYPE_VECTOR3I),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector3i_case2", script.resource_path, 9, TYPE_VECTOR3I, [
				GdFunctionArgument.new("value", TYPE_VECTOR3I, Vector3i.ONE),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector3i_case3", script.resource_path, 13, TYPE_VECTOR3I, [
				GdFunctionArgument.new("value", TYPE_VECTOR3I, Vector3i.ONE),
			])
		)


func test_default_args_basic_type_vector4() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeVector4DefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector4_case1", script.resource_path, 5, TYPE_VECTOR4, [
				GdFunctionArgument.new("value", TYPE_VECTOR4),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector4_case2", script.resource_path, 9, TYPE_VECTOR4, [
				GdFunctionArgument.new("value", TYPE_VECTOR4, Vector4.ONE),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector4_case3", script.resource_path, 13, TYPE_VECTOR4, [
				GdFunctionArgument.new("value", TYPE_VECTOR4, Vector4.ONE),
			])
		)


func test_default_args_basic_type_vector4i() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeVector4iDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector4i_case1", script.resource_path, 5, TYPE_VECTOR4I, [
				GdFunctionArgument.new("value", TYPE_VECTOR4I),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector4i_case2", script.resource_path, 9, TYPE_VECTOR4I, [
				GdFunctionArgument.new("value", TYPE_VECTOR4I, Vector4i.ONE),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_vector4i_case3", script.resource_path, 13, TYPE_VECTOR4I, [
				GdFunctionArgument.new("value", TYPE_VECTOR4I, Vector4i.ONE),
			])
		)


func test_default_args_basic_type_rect2() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeRect2DefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_rect2_case1", script.resource_path, 5, TYPE_RECT2, [
				GdFunctionArgument.new("value", TYPE_RECT2),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_rect2_case2", script.resource_path, 9, TYPE_RECT2, [
				GdFunctionArgument.new("value", TYPE_RECT2, Rect2()),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_rect2_case3", script.resource_path, 13, TYPE_RECT2, [
				GdFunctionArgument.new("value", TYPE_RECT2, Rect2()),
			])
		)
	assert_that(fds[3])\
		.is_equal(GdFunctionDescriptor
			.create("on_rect2_case4", script.resource_path, 17, TYPE_RECT2, [
				GdFunctionArgument.new("value", TYPE_RECT2, Rect2(Vector2.ONE, Vector2.ZERO)),
			])
		)
	assert_that(fds[4])\
		.is_equal(GdFunctionDescriptor
			.create("on_rect2_case5", script.resource_path, 21, TYPE_RECT2, [
				GdFunctionArgument.new("value", TYPE_RECT2, Rect2(Vector2(0, 1), Vector2(2, 3))),
			])
		)


func test_default_args_basic_type_transform2d() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/core/resources/parsing/functions/basic_build_in_types/ClassWithBasicTypeTransform2DDefaultArguments.gd")

	var fds := _parser.get_function_descriptors(script, [])
	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("on_transform2d_case1", script.resource_path, 5, TYPE_TRANSFORM2D, [
				GdFunctionArgument.new("value", TYPE_TRANSFORM2D),
			])
		)
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor
			.create("on_transform2d_case2", script.resource_path, 9, TYPE_TRANSFORM2D, [
				GdFunctionArgument.new("value", TYPE_TRANSFORM2D, Transform2D()),
			])
		)
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor
			.create("on_transform2d_case3", script.resource_path, 13, TYPE_TRANSFORM2D, [
				GdFunctionArgument.new("value", TYPE_TRANSFORM2D, Transform2D()),
			])
		)
	assert_that(fds[3])\
		.is_equal(GdFunctionDescriptor
			.create("on_transform2d_case4", script.resource_path, 17, TYPE_TRANSFORM2D, [
				GdFunctionArgument.new("value", TYPE_TRANSFORM2D, Transform2D(1.2, Vector2.ONE)),
			])
		)
