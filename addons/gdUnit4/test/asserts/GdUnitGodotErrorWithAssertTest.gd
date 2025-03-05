extends GdUnitTestSuite


var _catched_events :Array[GdUnitEvent] = []


func test_assert_method_with_enabled_global_error_report() -> void:
	ProjectSettings.set_setting(GdUnitSettings.REPORT_SCRIPT_ERRORS, true)
	await assert_error(do_a_fail).is_runtime_error('Assertion failed: test')


func test_assert_method_with_disabled_global_error_report() -> void:
	ProjectSettings.set_setting(GdUnitSettings.REPORT_SCRIPT_ERRORS, false)
	await assert_error(do_a_fail).is_runtime_error('Assertion failed: test')


@warning_ignore("assert_always_false")
func do_a_fail() -> void:
	if OS.is_debug_build():
		# On debug level we need to simulate the assert log entry, otherwise we stuck on a breakpoint
		if Engine.get_version_info().hex >= 0x40400:
			prints("""
			SCRIPT ERROR: Assertion failed: test
			   at: do_a_fail (res://addons/gdUnit4/test/asserts/GdUnitErrorAssertTest.gd:20)""".dedent())
		else:
			prints("""
			USER SCRIPT ERROR: Assertion failed: test
			   at: do_a_fail (res://addons/gdUnit4/test/asserts/GdUnitErrorAssertTest.gd:20)""".dedent())
	else:
		@warning_ignore("assert_always_false")
		assert(3 == 1, 'test')


func catch_test_events(event :GdUnitEvent) -> void:
	_catched_events.append(event)


func before() -> void:
	GdUnitSignals.instance().gdunit_event.connect(catch_test_events)


func after() -> void:
	# We expect no errors or failures, as we caught already the assert error by using the assert `assert_error` on the test case
	assert_array(_catched_events).extractv(extr("error_count"), extr("failed_count"))\
		.contains_exactly([tuple(0, 0), tuple(0,0), tuple(0,0), tuple(0,0)])
	GdUnitSignals.instance().gdunit_event.disconnect(catch_test_events)
