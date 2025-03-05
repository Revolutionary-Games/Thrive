extends GdUnitTestSuite

var _parser: GdScriptParser


static func build_tmp_script(source_code: String) -> GDScript:
	var script := GDScript.new()
	script.source_code = source_code.dedent()
	script.resource_path = GdUnitFileAccess.temp_dir() + "/tmp_%d.gd" % script.get_instance_id()
	var file := FileAccess.open(script.resource_path, FileAccess.WRITE)
	file.store_string(script.source_code)
	file.close()
	var error := script.reload()
	if error:
		push_error("Can't load temp script '%s', error: %s" % [source_code, error_string(error)])
		return null
	return script


func before() -> void:
	_parser = GdScriptParser.new()


func after() -> void:
	clean_temp_dir()


func test_parse_function_arguments() -> void:
	assert_dict(_parser._parse_function_arguments("func foo():")) \
		.has_size(0)

	assert_dict(_parser._parse_function_arguments("func foo() -> String:\n")) \
		.has_size(0)

	assert_dict(_parser._parse_function_arguments("func foo(arg1, arg2, name):")) \
		.has_size(3)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("name", GdFunctionArgument.UNDEFINED)

	assert_dict(_parser._parse_function_arguments('func foo(arg1 :int, arg2 :bool, name :String = "abc"):')) \
		.has_size(3)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("name", '"abc"')

	assert_dict(_parser._parse_function_arguments('func bar(arg1 :int, arg2 :int = 23, name :String = "test") -> String:')) \
		.has_size(3)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "23")\
		.contains_key_value("name", '"test"')

	assert_dict(_parser._parse_function_arguments("func foo(arg1, arg2=value(1,2,3), name:=foo()):")) \
		.has_size(3)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "value(1,2,3)")\
		.contains_key_value("name", "foo()")
	# enum as prefix in value name
	assert_dict(_parser._parse_function_arguments("func get_value( type := ENUM_A) -> int:"))\
		.has_size(1)\
		.contains_key_value("type", "ENUM_A")

	assert_dict(_parser._parse_function_arguments("func create_timer(timeout :float) -> Timer:")) \
		.has_size(1)\
		.contains_key_value("timeout", GdFunctionArgument.UNDEFINED)

	# array argument
	assert_dict(_parser._parse_function_arguments("func foo(a :int, b :int, parameters = [[1, 2], [3, 4], [5, 6]]):")) \
		.has_size(3)\
		.contains_key_value("a", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("b", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("parameters", "[[1, 2], [3, 4], [5, 6]]")

	assert_dict(_parser._parse_function_arguments("func test_values(a:Vector2, b:Vector2, expected:Vector2, test_parameters:=[[Vector2.ONE,Vector2.ONE,Vector2(1,1)]]):"))\
		.has_size(4)\
		.contains_key_value("a", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("b", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("expected", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("test_parameters", "[[Vector2.ONE,Vector2.ONE,Vector2(1,1)]]")


func test_parse_arguments_with_super_constructor() -> void:
	assert_dict(_parser._parse_function_arguments('func foo().foo("abc"):')).is_empty()
	assert_dict(_parser._parse_function_arguments('func foo(arg1 = "arg").foo("abc", arg1):'))\
		.has_size(1)\
		.contains_key_value("arg1",'"arg"')


func test_parse_arguments_default_build_in_type_string() -> void:
	assert_dict(_parser._parse_function_arguments('func foo(arg1 :String, arg2="default"):')) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", '"default"')

	assert_dict(_parser._parse_function_arguments('func foo(arg1 :String, arg2 :="default"):')) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", '"default"')

	assert_dict(_parser._parse_function_arguments('func foo(arg1 :String, arg2 :String ="default"):')) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", '"default"')


func test_parse_arguments_default_build_in_type_boolean() -> void:
	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2=false):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "false")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :=false):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "false")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :bool=false):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "false")


func test_parse_arguments_default_build_in_type_float() -> void:
	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2=3.14):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "3.14")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :=3.14):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "3.14")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :float=3.14):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "3.14")


func test_parse_arguments_default_build_in_type_array() -> void:
	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :Array=[]):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "[]")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :Array=Array()):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Array()")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :Array=[1, 2, 3]):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "[1, 2, 3]")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :=[1, 2, 3]):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "[1, 2, 3]")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2=[]):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "[]")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :Array=[1, 2, 3], arg3 := false):")) \
		.has_size(3)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "[1, 2, 3]")\
		.contains_key_value("arg3", "false")


func test_parse_arguments_default_build_in_type_color() -> void:
	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2=Color.RED):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Color.RED")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :=Color.RED):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Color.RED")

	assert_dict(_parser._parse_function_arguments("func foo(arg1 :String, arg2 :Color=Color.RED):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Color.RED")


func test_parse_arguments_default_build_in_type_vector() -> void:
	assert_dict(_parser._parse_function_arguments("func bar(arg1 :String, arg2 =Vector3.FORWARD):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Vector3.FORWARD")

	assert_dict(_parser._parse_function_arguments("func bar(arg1 :String, arg2 :=Vector3.FORWARD):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Vector3.FORWARD")

	assert_dict(_parser._parse_function_arguments("func bar(arg1 :String, arg2 :Vector3=Vector3.FORWARD):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Vector3.FORWARD")


func test_parse_arguments_default_build_in_type_AABB() -> void:
	assert_dict(_parser._parse_function_arguments("func bar(arg1 :String, arg2 := AABB()):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "AABB()")

	assert_dict(_parser._parse_function_arguments("func bar(arg1 :String, arg2 :AABB=AABB()):")) \
		.has_size(2)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "AABB()")


func test_parse_arguments_default_build_in_types() -> void:
	assert_dict(_parser._parse_function_arguments("func bar(arg1 :String, arg2 := Vector3.FORWARD, aabb := AABB()):")) \
		.has_size(3)\
		.contains_key_value("arg1", GdFunctionArgument.UNDEFINED)\
		.contains_key_value("arg2", "Vector3.FORWARD")\
		.contains_key_value("aabb", "AABB()")


func test_parse_arguments_fuzzers() -> void:
	assert_dict(_parser._parse_function_arguments("func test_foo(fuzzer_a = fuzz_a(), fuzzer_b := fuzz_b(), fuzzer_c :Fuzzer = fuzz_c(), fuzzer_iterations = 234, fuzzer_seed = 100):")) \
		.has_size(5)\
		.contains_key_value("fuzzer_a", "fuzz_a()")\
		.contains_key_value("fuzzer_b", "fuzz_b()")\
		.contains_key_value("fuzzer_c", "fuzz_c()")\
		.contains_key_value("fuzzer_iterations", "234")\
		.contains_key_value("fuzzer_seed", "100")


class TestObject:
	var x: int

func test_parse_func_name() -> void:
	assert_str(_parser.parse_func_name("func foo():")).is_equal("foo")
	assert_str(_parser.parse_func_name("static func foo():")).is_equal("foo")
	assert_str(_parser.parse_func_name("func a() -> String:")).is_equal("a")
	# function name contains tokens e.g func or class
	assert_str(_parser.parse_func_name("func foo_func_class():")).is_equal("foo_func_class")
	# should fail
	assert_str(_parser.parse_func_name("#func foo():")).is_empty()
	assert_str(_parser.parse_func_name("var x")).is_empty()


func test_load_source_code_inner_class_AtmosphereData() -> void:
	var base_class := AdvancedTestClass.new()
	@warning_ignore("unsafe_cast")
	var rows := _parser._load_inner_class(base_class.get_script() as GDScript, "AtmosphereData")
	var file_content := resource_as_string("res://addons/gdUnit4/test/core/resources/AtmosphereData.txt")
	assert_that(rows).is_equal(file_content)


func test_load_source_code_inner_class_SoundData() -> void:
	var base_class := AdvancedTestClass.new()
	@warning_ignore("unsafe_cast")
	var rows := _parser._load_inner_class(base_class.get_script() as GDScript, "SoundData")
	var file_content := resource_as_string("res://addons/gdUnit4/test/core/resources/SoundData.txt")
	assert_that(rows).is_equal(file_content)


func test_load_source_code_inner_class_Area4D() -> void:
	var base_class := AdvancedTestClass.new()
	@warning_ignore("unsafe_cast")
	var rows := _parser._load_inner_class(base_class.get_script() as GDScript, "Area4D")
	var file_content := resource_as_string("res://addons/gdUnit4/test/core/resources/Area4D.txt")
	assert_that(rows).is_equal(file_content)


func test_extract_function_signature() -> void:
	var script :GDScript = load("res://addons/gdUnit4/test/mocker/resources/ClassWithCustomFormattings.gd")
	var rows := script.source_code.split("\n")

	assert_that(_parser.extract_func_signature(rows, 12))\
		.is_equal("""
			func a1(set_name:String, path:String="", load_on_init:bool=false,
				set_auto_save:bool=false, set_network_sync:bool=false
			) -> void:""".dedent().trim_prefix("\n"))
	assert_that(_parser.extract_func_signature(rows, 19))\
		.is_equal("""
			func a2(set_name:String, path:String="", load_on_init:bool=false,
				set_auto_save:bool=false, set_network_sync:bool=false
			) -> 	void:""".dedent().trim_prefix("\n"))
	assert_that(_parser.extract_func_signature(rows, 26))\
		.is_equal("""
			func a3(set_name:String, path:String="", load_on_init:bool=false,
				set_auto_save:bool=false, set_network_sync:bool=false
			) -> void:""".dedent().trim_prefix("\n"))
	assert_that(_parser.extract_func_signature(rows, 33))\
		.is_equal("""
			func a4(set_name:String,
				path:String="",
				load_on_init:bool=false,
				set_auto_save:bool=false,
				set_network_sync:bool=false
			) -> void:""".dedent().trim_prefix("\n"))
	assert_that(_parser.extract_func_signature(rows, 43))\
		.is_equal("""
			func a5(
				value : Array,
				expected : String,
				test_parameters : Array = [
					[ ["a"], "a" ],
					[ ["a", "very", "long", "argument"], "a very long argument" ],
				]
			) -> void:""".dedent().trim_prefix("\n"))


func test_strip_leading_spaces() -> void:
	assert_str(GdScriptParser.TokenInnerClass._strip_leading_spaces("")).is_empty()
	assert_str(GdScriptParser.TokenInnerClass._strip_leading_spaces(" ")).is_empty()
	assert_str(GdScriptParser.TokenInnerClass._strip_leading_spaces("    ")).is_empty()
	assert_str(GdScriptParser.TokenInnerClass._strip_leading_spaces("	 ")).is_equal("	 ")
	assert_str(GdScriptParser.TokenInnerClass._strip_leading_spaces("var x=")).is_equal("var x=")
	assert_str(GdScriptParser.TokenInnerClass._strip_leading_spaces("class foo")).is_equal("class foo")


func test_extract_clazz_name() -> void:
	assert_str(_parser.extract_clazz_name("class SoundData:\n")).is_equal("SoundData")
	assert_str(_parser.extract_clazz_name("class SoundData extends Node:\n")).is_equal("SoundData")


func test_parse_func_description() -> void:
	var script := build_tmp_script("""

		@warning_ignore("untyped_declaration")
		func foo0():
			pass

		@warning_ignore("unused_parameter")
		static func foo1(arg1 :int, arg2:=false) -> String:
			return ""

		@warning_ignore("untyped_declaration", "unused_parameter")
		static func foo2(arg1 :int, arg2:=true):
			pass
	""")
	var fds := _parser.get_function_descriptors(script)

	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor.create("foo0", script.resource_path, 4, GdObjects.TYPE_VOID))

	# static function
	assert_that(fds[1])\
		.is_equal(GdFunctionDescriptor.create_static("foo1", script.resource_path, 8, TYPE_STRING, [
			GdFunctionArgument.new("arg1", TYPE_INT),
			GdFunctionArgument.new("arg2", TYPE_BOOL, false)
		]))

	# static function without return type
	assert_that(fds[2])\
		.is_equal(GdFunctionDescriptor.create_static("foo2", script.resource_path, 12, GdObjects.TYPE_VOID, [
			GdFunctionArgument.new("arg1", TYPE_INT),
			GdFunctionArgument.new("arg2", TYPE_BOOL, true)
		]))


func test_get_function_descriptors_return_type_enum() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/mocker/resources/ClassWithEnumReturnTypes.gd")
	var fds := _parser.get_function_descriptors(script, ["get_enum"])

	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("get_enum", script.resource_path, 15, GdObjects.TYPE_ENUM)
			.with_return_class("ClassWithEnumReturnTypes.TEST_ENUM")
		)


func test_parse_func_description_return_type_internal_class_enum() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/mocker/resources/ClassWithEnumReturnTypes.gd")
	var fds := _parser.get_function_descriptors(script, ["get_inner_class_enum"])

	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("get_inner_class_enum", script.resource_path, 25, GdObjects.TYPE_ENUM)
			.with_return_class("ClassWithEnumReturnTypes.InnerClass.TEST_ENUM")
		)


func test_parse_func_description_return_type_external_class_enum() -> void:
	var script: GDScript = load("res://addons/gdUnit4/test/mocker/resources/ClassWithEnumReturnTypes.gd")
	var fds := _parser.get_function_descriptors(script, ["get_external_class_enum"])

	assert_that(fds[0])\
		.is_equal(GdFunctionDescriptor
			.create("get_external_class_enum", script.resource_path, 20, GdObjects.TYPE_ENUM)
			.with_return_class("CustomEnums.TEST_ENUM")
		)


func test_parse_class_inherits() -> void:
	var clazz_path := GdObjects.extract_class_path(CustomClassExtendsCustomClass)
	var clazz_name := GdObjects.extract_class_name_from_class_path(clazz_path)
	var result := _parser.parse(clazz_name, clazz_path)
	assert_result(result).is_success()

	# verify class extraction
	var clazz_desccriptor :GdClassDescriptor = result.value()
	assert_object(clazz_desccriptor).is_not_null()
	assert_str(clazz_desccriptor.name()).is_equal("CustomClassExtendsCustomClass")
	assert_bool(clazz_desccriptor.is_inner_class()).is_false()
	assert_array(clazz_desccriptor.functions())\
		.contains_exactly([
			GdFunctionDescriptor.create("foo2", "res://addons/gdUnit4/test/mocker/resources/CustomClassExtendsCustomClass.gd", 6, GdObjects.TYPE_VARIANT),
			GdFunctionDescriptor.create("bar2", "res://addons/gdUnit4/test/mocker/resources/CustomClassExtendsCustomClass.gd", 9, TYPE_STRING),
			GdFunctionDescriptor.create("foo", "res://addons/gdUnit4/test/mocker/resources/CustomResourceTestClass.gd", 4, TYPE_STRING),
			GdFunctionDescriptor.create("foo_void", "res://addons/gdUnit4/test/mocker/resources/CustomResourceTestClass.gd", 10, GdObjects.TYPE_VOID),
			GdFunctionDescriptor.create("bar", "res://addons/gdUnit4/test/mocker/resources/CustomResourceTestClass.gd", 13, TYPE_STRING, [
				GdFunctionArgument.new("arg1", TYPE_INT),
				GdFunctionArgument.new("arg2", TYPE_INT, 23),
				GdFunctionArgument.new("name", TYPE_STRING, "test"),
			]),
			GdFunctionDescriptor.create("foo5", "res://addons/gdUnit4/test/mocker/resources/CustomResourceTestClass.gd", 17, GdObjects.TYPE_VOID),
		])


func test_get_class_name_pascal_case() -> void:
	assert_str(_parser.get_class_name(load("res://addons/gdUnit4/test/core/resources/naming_conventions/PascalCaseWithClassName.gd") as GDScript))\
		.is_equal("PascalCaseWithClassName")
	assert_str(_parser.get_class_name(load("res://addons/gdUnit4/test/core/resources/naming_conventions/PascalCaseWithoutClassName.gd") as GDScript))\
		.is_equal("PascalCaseWithoutClassName")


func test_get_class_name_snake_case() -> void:
	assert_str(_parser.get_class_name(load("res://addons/gdUnit4/test/core/resources/naming_conventions/snake_case_with_class_name.gd") as GDScript))\
		.is_equal("SnakeCaseWithClassName")
	assert_str(_parser.get_class_name(load("res://addons/gdUnit4/test/core/resources/naming_conventions/snake_case_without_class_name.gd") as GDScript))\
		.is_equal("SnakeCaseWithoutClassName")


func test_get_class_with_extends_in_same_line() -> void:
	assert_str(_parser.get_class_name(load("res://addons/gdUnit4/test/core/resources/naming_conventions/extends_on_same_line.gd") as GDScript))\
		.is_equal("ClassNameExtendsInSameLine")


func test_is_func_end() -> void:
	assert_bool(_parser.is_func_end("")).is_false()
	assert_bool(_parser.is_func_end("func test_a():")).is_true()
	assert_bool(_parser.is_func_end("func test_a() -> void:")).is_true()
	assert_bool(_parser.is_func_end("func test_a(arg1) :")).is_true()
	assert_bool(_parser.is_func_end("func test_a(arg1 ): ")).is_true()
	assert_bool(_parser.is_func_end("func test_a(arg1 ):	")).is_true()
	assert_bool(_parser.is_func_end("	):")).is_true()
	assert_bool(_parser.is_func_end("		):")).is_true()
	assert_bool(_parser.is_func_end("	-> void:")).is_true()
	assert_bool(_parser.is_func_end("		) -> void :")).is_true()
	assert_bool(_parser.is_func_end("func test_a(arg1, arg2 = {1:2} ):")).is_true()


func test_extract_func_signature_multiline() -> void:
	var source_code := """

		func test_parameterized(a: int, b :int, c :int, expected :int, test_parameters = [
			[1, 2, 3, 6],
			[3, 4, 5, 11],
			[6, 7, 8, 21] ]):

			assert_that(a+b+c).is_equal(expected)
		""".dedent().split("\n")

	var fs := _parser.extract_func_signature(source_code, 0)

	assert_that(fs).is_equal("""
		func test_parameterized(a: int, b :int, c :int, expected :int, test_parameters = [
			[1, 2, 3, 6],
			[3, 4, 5, 11],
			[6, 7, 8, 21] ]):"""
			.dedent()
			.trim_prefix("\n")
		)


func test_parse_func_description_paramized_test() -> void:
	var script := build_tmp_script("""
		@warning_ignore("unused_parameter")
		func test_parameterized(a: int, b: int, c: int, expected: int, test_parameters := [[1,2,3,6],[3,4,5,11],[6,7,8,21]]) -> Variant:
			return null
	""")
	var fds := GdScriptParser.new().get_function_descriptors(script, ["test_parameterized"])

	assert_that(fds[0]).is_equal(GdFunctionDescriptor.create("test_parameterized", script.resource_path, 3, GdObjects.TYPE_VARIANT, [
		GdFunctionArgument.new("a", TYPE_INT),
		GdFunctionArgument.new("b", TYPE_INT),
		GdFunctionArgument.new("c", TYPE_INT),
		GdFunctionArgument.new("expected", TYPE_INT),
		GdFunctionArgument.new("test_parameters", TYPE_ARRAY, "[[1,2,3,6],[3,4,5,11],[6,7,8,21]]"),
	]))


func test_parse_func_description_paramized_test_with_comments() -> void:
	var source_code := """
		func test_parameterized(a: int, b :int, c :int, expected :int, test_parameters = [
			# before data set
			[1, 2, 3, 6], # after data set
			# between data sets
			[3, 4, 5, 11],
			[6, 7, 'string #ABCD', 21], # dataset with [comment] singn
			[6, 7, "string #ABCD", 21] # dataset with "#comment" singn
			#eof
		]):
			pass
		""".dedent().split("\n")

	var fs := _parser.extract_func_signature(source_code, 0)

	assert_that(fs).is_equal("""
		func test_parameterized(a: int, b :int, c :int, expected :int, test_parameters = [
			[1, 2, 3, 6],
			[3, 4, 5, 11],
			[6, 7, 'string #ABCD', 21],
			[6, 7, "string #ABCD", 21]
		]):"""
			.dedent()
			.trim_prefix("\n")
		)


func test_parse_func_descriptor_with_fuzzers() -> void:
	# using a mixure of typed and untyped default values
	var script := build_tmp_script("""
		func fuzz_a() -> Fuzzer:
			return Fuzzers.rangef(0, 10)

		func fuzz_b() -> Fuzzer:
			return Fuzzers.rangef(0, 10)

		func fuzz_c() -> Fuzzer:
			return Fuzzers.rangef(0, 10)

		@warning_ignore("untyped_declaration", "unused_parameter")
		func test_foo(fuzzer_a = fuzz_a(), fuzzer_b := fuzz_b(),
			fuzzer_c :Fuzzer = fuzz_c(),
			fuzzer = Fuzzers.rangei(-23, 22),
			fuzzer_iterations = 234,
			fuzzer_seed := 100):
			pass
	""")
	var fds := _parser.get_function_descriptors(script, ["test_foo"])

	assert_that(fds[0]).is_equal(GdFunctionDescriptor.create("test_foo", script.resource_path, 12, GdObjects.TYPE_VOID, [
		# all fuzzer must by type TYPE_FUZZER
		GdFunctionArgument.new("fuzzer_a", GdObjects.TYPE_FUZZER, "fuzz_a()"),
		GdFunctionArgument.new("fuzzer_b", GdObjects.TYPE_FUZZER, "fuzz_b()"),
		GdFunctionArgument.new("fuzzer_c", GdObjects.TYPE_FUZZER, "fuzz_c()"),
		GdFunctionArgument.new("fuzzer", GdObjects.TYPE_FUZZER, "Fuzzers.rangei(-23, 22)"),
		# untyped arg is TYPE_VARIANT
		GdFunctionArgument.new("fuzzer_iterations", GdObjects.TYPE_VARIANT, "234"),
		# typed is TYPE_INT
		GdFunctionArgument.new("fuzzer_seed", TYPE_INT, 100)
	]))
