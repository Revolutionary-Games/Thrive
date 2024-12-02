## The test case startup hook implementation.[br]
## It executes the 'test_before()' block from the test-suite.
class_name GdUnitTestCaseBeforeStage
extends IGdUnitExecutionStage

var _call_stage :bool


func _init(call_stage := true) -> void:
	_call_stage = call_stage


func _execute(context :GdUnitExecutionContext) -> void:
	var test_suite := context.test_suite

	fire_event(GdUnitEvent.new()\
		.test_before(context.get_test_suite_path(), context.get_test_suite_name(), context.get_test_case_name()))
	if _call_stage:
		@warning_ignore("redundant_await")
		await test_suite.before_test()
	context.error_monitor_start()
