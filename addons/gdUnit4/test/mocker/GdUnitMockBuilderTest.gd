# GdUnit generated TestSuite
class_name GdUnitMockBuilderTest
extends GdUnitTestSuite

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/mocking/GdUnitMockBuilder.gd'


# helper to get function descriptor
func get_function_description(clazz_name :String, method_name :String) -> GdFunctionDescriptor:
	var method_list :Array = ClassDB.class_get_method_list(clazz_name)
	for method_descriptor :Dictionary in method_list:
		if method_descriptor["name"] == method_name:
			return GdFunctionDescriptor.extract_from(method_descriptor)
	return null


func test_double_return_typed_function_without_arg() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)
	# String get_class() const
	var fd := get_function_description("Object", "get_class")
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		@warning_ignore("native_method_override")
		func get_class() -> String:
			var args__: Array = ["get_class", ]

			if __is_prepare_return_value():
				__save_function_return_value(args__)
				return ""

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return ""
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("get_class", args__):
				@warning_ignore("unsafe_call_argument")
				return super()

			return __get_mocked_return_value_or_default(args__, "")

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_double_return_typed_function_with_args() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)
	# bool is_connected(signal: String, callable_: Callable)) const
	var fd := get_function_description("Object", "is_connected")
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		@warning_ignore("native_method_override")
		func is_connected(signal_, callable_) -> bool:
			var args__: Array = ["is_connected", signal_, callable_]

			if __is_prepare_return_value():
				__save_function_return_value(args__)
				return false

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return false
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("is_connected", args__):
				@warning_ignore("unsafe_call_argument")
				return super(signal_, callable_)

			return __get_mocked_return_value_or_default(args__, false)

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_double_return_untyped_function_with_args() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)

	# void disconnect(signal: StringName, callable: Callable)
	var fd := get_function_description("Object", "disconnect")
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		@warning_ignore("native_method_override")
		func disconnect(signal_, callable_) -> void:
			var args__: Array = ["disconnect", signal_, callable_]

			if __is_prepare_return_value():
				if false:
					push_error("Mocking a void function 'disconnect(<args>) -> void:' is not allowed.")
				return

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("disconnect"):
				@warning_ignore("unsafe_call_argument")
				super(signal_, callable_)

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_double_int_function_with_varargs() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)
	# Error emit_signal(signal: StringName, ...) vararg
	var fd := get_function_description("Object", "emit_signal")
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		@warning_ignore("native_method_override")
		func emit_signal(signal_, vararg0_="__null__", vararg1_="__null__", vararg2_="__null__", vararg3_="__null__", vararg4_="__null__", vararg5_="__null__", vararg6_="__null__", vararg7_="__null__", vararg8_="__null__", vararg9_="__null__") -> Error:
			var varargs__: Array = __get_verifier().filter_vargs([vararg0_, vararg1_, vararg2_, vararg3_, vararg4_, vararg5_, vararg6_, vararg7_, vararg8_, vararg9_])
			var args__: Array = ["emit_signal", signal_] + varargs__

			if __is_prepare_return_value():
				if false:
					push_error("Mocking a void function 'emit_signal(<args>) -> void:' is not allowed.")
				__save_function_return_value(args__)
				return OK as Error

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return OK as Error
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("emit_signal", args__):
				match varargs__.size():
					@warning_ignore("unsafe_call_argument")
					0: return super(signal_)
					@warning_ignore("unsafe_call_argument")
					1: return super(signal_, varargs__[0])
					@warning_ignore("unsafe_call_argument")
					2: return super(signal_, varargs__[0], varargs__[1])
					@warning_ignore("unsafe_call_argument")
					3: return super(signal_, varargs__[0], varargs__[1], varargs__[2])
					@warning_ignore("unsafe_call_argument")
					4: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3])
					@warning_ignore("unsafe_call_argument")
					5: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4])
					@warning_ignore("unsafe_call_argument")
					6: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5])
					@warning_ignore("unsafe_call_argument")
					7: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6])
					@warning_ignore("unsafe_call_argument")
					8: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7])
					@warning_ignore("unsafe_call_argument")
					9: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8])
					@warning_ignore("unsafe_call_argument")
					10: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8], varargs__[9])

			return __get_mocked_return_value_or_default(args__, OK)

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_double_untyped_function_with_varargs() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)

	# void emit_custom(signal_name, args__ ...) vararg const
	var fd := GdFunctionDescriptor.new("emit_custom", 10, false, false, false, TYPE_NIL, "",
		[GdFunctionArgument.new("signal", TYPE_SIGNAL)],
		GdFunctionDescriptor._build_varargs(true))
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		func emit_custom(signal_, vararg0_="__null__", vararg1_="__null__", vararg2_="__null__", vararg3_="__null__", vararg4_="__null__", vararg5_="__null__", vararg6_="__null__", vararg7_="__null__", vararg8_="__null__", vararg9_="__null__") -> void:
			var varargs__: Array = __get_verifier().filter_vargs([vararg0_, vararg1_, vararg2_, vararg3_, vararg4_, vararg5_, vararg6_, vararg7_, vararg8_, vararg9_])
			var args__: Array = ["emit_custom", signal_] + varargs__

			if __is_prepare_return_value():
				if false:
					push_error("Mocking a void function 'emit_custom(<args>) -> void:' is not allowed.")
				__save_function_return_value(args__)
				return null

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return null
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("emit_custom", args__):
				match varargs__.size():
					@warning_ignore("unsafe_call_argument")
					0: return super(signal_)
					@warning_ignore("unsafe_call_argument")
					1: return super(signal_, varargs__[0])
					@warning_ignore("unsafe_call_argument")
					2: return super(signal_, varargs__[0], varargs__[1])
					@warning_ignore("unsafe_call_argument")
					3: return super(signal_, varargs__[0], varargs__[1], varargs__[2])
					@warning_ignore("unsafe_call_argument")
					4: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3])
					@warning_ignore("unsafe_call_argument")
					5: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4])
					@warning_ignore("unsafe_call_argument")
					6: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5])
					@warning_ignore("unsafe_call_argument")
					7: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6])
					@warning_ignore("unsafe_call_argument")
					8: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7])
					@warning_ignore("unsafe_call_argument")
					9: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8])
					@warning_ignore("unsafe_call_argument")
					10: return super(signal_, varargs__[0], varargs__[1], varargs__[2], varargs__[3], varargs__[4], varargs__[5], varargs__[6], varargs__[7], varargs__[8], varargs__[9])

			return __get_mocked_return_value_or_default(args__, null)

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_double_virtual_script_function_without_arg() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)

	# void _ready() virtual
	var fd := get_function_description("Node", "_ready")
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		@warning_ignore("native_method_override")
		func _ready() -> void:
			var args__: Array = ["_ready", ]

			if __is_prepare_return_value():
				if false:
					push_error("Mocking a void function '_ready(<args>) -> void:' is not allowed.")
				return

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("_ready"):
				@warning_ignore("unsafe_call_argument")
				super()

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_double_virtual_script_function_with_arg() -> void:
	var doubler := GdUnitMockFunctionDoubler.new(false)

	# void _input(event: InputEvent) virtual
	var fd := get_function_description("Node", "_input")
	var expected := """
		@warning_ignore('shadowed_variable', 'untyped_declaration')
		@warning_ignore("native_method_override")
		func _input(event_) -> void:
			var args__: Array = ["_input", event_]

			if __is_prepare_return_value():
				if false:
					push_error("Mocking a void function '_input(<args>) -> void:' is not allowed.")
				return

			# verify block
			var __verifier := __get_verifier()
			if __verifier != null:
				if __verifier.is_verify_interactions():
					__verifier.verify_interactions(args__)
					return
				else:
					__verifier.save_function_interaction(args__)

			if __do_call_real_func("_input"):
				@warning_ignore("unsafe_call_argument")
				super(event_)

	""".dedent().trim_prefix("\n")
	assert_str("\n".join(doubler.double(fd))).is_equal(expected)


func test_mock_on_script_path_without_class_name() -> void:
	var instance :Object = (load("res://addons/gdUnit4/test/mocker/resources/ClassWithoutNameA.gd") as GDScript).new()
	var script := GdUnitMockBuilder.mock_on_script(instance, "res://addons/gdUnit4/test/mocker/resources/ClassWithoutNameA.gd", [], true);
	assert_str(script.resource_name).starts_with("MockClassWithoutNameA")
	assert_that(script.get_instance_base_type()).is_equal("Resource")
	# finally check the mocked script is valid
	assert_int(script.reload()).is_equal(OK)


func test_mock_on_script_path_with_custom_class_name() -> void:
	# the class contains a class_name definition
	var instance :Object = (load("res://addons/gdUnit4/test/mocker/resources/ClassWithCustomClassName.gd") as GDScript).new()
	var script := GdUnitMockBuilder.mock_on_script(instance, "res://addons/gdUnit4/test/mocker/resources/ClassWithCustomClassName.gd", [], false);
	assert_str(script.resource_name).starts_with("MockGdUnitTestCustomClassName")
	assert_that(script.get_instance_base_type()).is_equal("Resource")
	# finally check the mocked script is valid
	assert_int(script.reload()).is_equal(OK)


func test_mock_on_class_with_class_name() -> void:
	var script := GdUnitMockBuilder.mock_on_script(ClassWithNameA.new(), ClassWithNameA, [], false);
	assert_str(script.resource_name).starts_with("MockClassWithNameA")
	assert_that(script.get_instance_base_type()).is_equal("Resource")
	# finally check the mocked script is valid
	assert_int(script.reload()).is_equal(OK)


func test_mock_on_class_with_custom_class_name() -> void:
	# the class contains a class_name definition
	var script := GdUnitMockBuilder.mock_on_script(GdUnit_Test_CustomClassName.new(), GdUnit_Test_CustomClassName, [], false);
	assert_str(script.resource_name).starts_with("MockGdUnitTestCustomClassName")
	assert_that(script.get_instance_base_type()).is_equal("Resource")
	# finally check the mocked script is valid
	assert_int(script.reload()).is_equal(OK)
