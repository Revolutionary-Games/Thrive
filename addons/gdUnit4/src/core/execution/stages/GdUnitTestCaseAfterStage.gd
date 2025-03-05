## The test case shutdown hook implementation.[br]
## It executes the 'test_after()' block from the test-suite.
class_name GdUnitTestCaseAfterStage
extends IGdUnitExecutionStage


var _call_stage: bool


func _init(call_stage := true) -> void:
	_call_stage = call_stage


func _execute(context: GdUnitExecutionContext) -> void:
	var test_suite := context.test_suite

	if _call_stage:
		@warning_ignore("redundant_await")
		await test_suite.after_test()

	await context.gc()
	await context.error_monitor_stop()

	var reports := context.build_reports()

	if context.is_skipped():
		fire_test_skipped(context)
	else:
		fire_event(GdUnitEvent.new().test_after(context.test_case.id(), context.get_execution_statistics(), reports))


func fire_test_skipped(context: GdUnitExecutionContext) -> void:
	var test_case := context.test_case
	var statistics := {
		GdUnitEvent.ORPHAN_NODES: 0,
		GdUnitEvent.ELAPSED_TIME: 0,
		GdUnitEvent.WARNINGS: false,
		GdUnitEvent.ERRORS: false,
		GdUnitEvent.ERROR_COUNT: 0,
		GdUnitEvent.FAILED: false,
		GdUnitEvent.FAILED_COUNT: 0,
		GdUnitEvent.SKIPPED: true,
		GdUnitEvent.SKIPPED_COUNT: 1,
	}
	var report := GdUnitReport.new() \
		.create(GdUnitReport.SKIPPED, test_case.line_number(), GdAssertMessages.test_skipped(test_case.skip_info()))
	fire_event(GdUnitEvent.new().test_after(test_case.id(), statistics, [report]))
