# GdUnit generated TestSuite
class_name GdUnitGodotErrorAssertImplTest
extends GdUnitTestSuite
@warning_ignore('unused_parameter')
@warning_ignore('return_value_discarded')

# TestSuite generated from
const __source = 'res://addons/gdUnit4/src/asserts/GdUnitGodotErrorAssertImpl.gd'


class GodotErrorTestClass:

	func test(value :int) -> void:
		match value:
			0:
				@warning_ignore("assert_always_true")
				assert(true, "no error" )
			1: # failing assert
				await (Engine.get_main_loop() as SceneTree).process_frame
				if OS.is_debug_build():
					# do not break the debug session we simmulate a assert by writing the error manually
					if Engine.get_version_info().hex >= 0x40400:
						prints("""
							SCRIPT ERROR: Assertion failed: this is an assert error
							   at: GodotErrorTestClass.test (res://addons/gdUnit4/test/asserts/GdUnitGodotErrorAssertImplTest.gd:18)
						""".dedent())
					else:
						prints("""
							USER SCRIPT ERROR: Assertion failed: this is an assert error
							   at: GodotErrorTestClass.test (res://addons/gdUnit4/test/asserts/GdUnitGodotErrorAssertImplTest.gd:18)
						""".dedent())
				else:
					assert(false, "this is an assert error" )
			2: # push_warning
				push_warning('this is an push_warning')
			3: # push_error
				push_error('this is an push_error')
				pass
			4: # runtime error
				if OS.is_debug_build():
					# do not break the debug session we simmulate a assert by writing the error manually
					if Engine.get_version_info().hex >= 0x40400:
						prints("""
							SCRIPT ERROR: Division by zero error in operator '/'.
							   at: GodotErrorTestClass.test (res://addons/gdUnit4/test/asserts/GdUnitGodotErrorAssertImplTest.gd:32)
						""".dedent())
					else:
						prints("""
							USER SCRIPT ERROR: Division by zero error in operator '/'.
							   at: GodotErrorTestClass.test (res://addons/gdUnit4/test/asserts/GdUnitGodotErrorAssertImplTest.gd:32)
						""".dedent())
				else:
					var a := 0
					@warning_ignore("integer_division")
					@warning_ignore("unused_variable")
					var x := 1/a


var _save_is_report_push_errors :bool
var _save_is_report_script_errors :bool


# skip see https://github.com/godotengine/godot/issues/80292
func before() -> void:
	_save_is_report_push_errors = GdUnitSettings.is_report_push_errors()
	_save_is_report_script_errors = GdUnitSettings.is_report_script_errors()
	# disable default error reporting for testing
	ProjectSettings.set_setting(GdUnitSettings.REPORT_PUSH_ERRORS, false)
	ProjectSettings.set_setting(GdUnitSettings.REPORT_SCRIPT_ERRORS, false)


func after() -> void:
	ProjectSettings.set_setting(GdUnitSettings.REPORT_PUSH_ERRORS, _save_is_report_push_errors)
	ProjectSettings.set_setting(GdUnitSettings.REPORT_SCRIPT_ERRORS, _save_is_report_script_errors)


func after_test() -> void:
	# Cleanup report artifacts
	GdUnitThreadManager.get_current_context().get_execution_context().error_monitor._entries.clear()


func test_invalid_callable() -> void:
	assert_failure(func() -> void: assert_error(Callable()).is_success())\
		.is_failed()\
		.has_message("Invalid Callable 'null::null'")


func test_is_success() -> void:
	await assert_error(func() -> void: await GodotErrorTestClass.new().test(0)).is_success()

	var assert_ := await assert_failure_await(func() -> void:
		await assert_error(func() -> void: await GodotErrorTestClass.new().test(1)).is_success())
	assert_.is_failed().has_message("""
		Expecting: no error's are ocured.
			but found: 'Assertion failed: this is an assert error'
		""".dedent().trim_prefix("\n"))


func test_is_assert_failed() -> void:
	await assert_error(func() -> void: await GodotErrorTestClass.new().test(1))\
		.is_runtime_error('Assertion failed: this is an assert error')

	var assert_ := await assert_failure_await(func() -> void:
		await assert_error(func() -> void: GodotErrorTestClass.new().test(0)).is_runtime_error('Assertion failed: this is an assert error'))
	assert_.is_failed().has_message("""
		Expecting: a runtime error is triggered.
			message: 'Assertion failed: this is an assert error'
			found: no errors
		""".dedent().trim_prefix("\n"))


func test_is_push_warning() -> void:
	await assert_error(func() -> void: GodotErrorTestClass.new().test(2))\
		.is_push_warning('this is an push_warning')

	var assert_ := await assert_failure_await(func() -> void:
		await assert_error(func() -> void: GodotErrorTestClass.new().test(0)).is_push_warning('this is an push_warning'))
	assert_.is_failed().has_message("""
		Expecting: push_warning() is called.
			message: 'this is an push_warning'
			found: no errors
		""".dedent().trim_prefix("\n"))


func test_is_push_error() -> void:
	await assert_error(func() -> void: GodotErrorTestClass.new().test(3))\
		.is_push_error('this is an push_error')

	var assert_ := await assert_failure_await(func() -> void:
		await assert_error(func() -> void: GodotErrorTestClass.new().test(0)).is_push_error('this is an push_error'))
	assert_.is_failed().has_message("""
		Expecting: push_error() is called.
			message: 'this is an push_error'
			found: no errors
		""".dedent().trim_prefix("\n"))


func test_is_runtime_error() -> void:
	await assert_error(func() -> void: GodotErrorTestClass.new().test(4))\
		.is_runtime_error("Division by zero error in operator '/'.")

	var assert_ := await assert_failure_await(func() -> void:
		await assert_error(func() -> void: GodotErrorTestClass.new().test(0)).is_runtime_error("Division by zero error in operator '/'."))
	assert_.is_failed().has_message("""
		Expecting: a runtime error is triggered.
			message: 'Division by zero error in operator '/'.'
			found: no errors
		""".dedent().trim_prefix("\n"))
