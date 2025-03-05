## The executor to run a test-suite
class_name GdUnitTestSuiteExecutor


# preload all asserts here
@warning_ignore("unused_private_class_variable")
var _assertions := GdUnitAssertions.new()
var _executeStage := GdUnitTestSuiteExecutionStage.new()


func _init(debug_mode :bool = false) -> void:
	_executeStage.set_debug_mode(debug_mode)


func execute(test_suite :GdUnitTestSuite) -> void:
	var orphan_detection_enabled := GdUnitSettings.is_verbose_orphans()
	if not orphan_detection_enabled:
		prints("!!! Reporting orphan nodes is disabled. Please check GdUnit settings.")

	(Engine.get_main_loop() as SceneTree).root.call_deferred("add_child", test_suite)
	await (Engine.get_main_loop() as SceneTree).process_frame
	await _executeStage.execute(GdUnitExecutionContext.of_test_suite(test_suite))


func run_and_wait(tests: Array[GdUnitTestCase]) -> void:
	# first we group all tests by his parent suite
	var grouped_by_suites := GdArrayTools.group_by(tests, func(test: GdUnitTestCase) -> String:
		return test.source_file
	)
	var scanner := GdUnitTestSuiteScanner.new()
	for suite_path: String in grouped_by_suites.keys():
		@warning_ignore("unsafe_call_argument")
		var suite_tests: Array[GdUnitTestCase] = Array(grouped_by_suites[suite_path], TYPE_OBJECT, "RefCounted", GdUnitTestCase)
		var script := GdUnitTestSuiteScanner.load_with_disabled_warnings(suite_path)
		var test_suite := scanner.load_suite(script, suite_tests)
		await execute(test_suite)


func fail_fast(enabled :bool) -> void:
	_executeStage.fail_fast(enabled)
