## The test case execution stage.[br]
class_name GdUnitTestCaseSingleExecutionStage
extends IGdUnitExecutionStage


var _stage_before :IGdUnitExecutionStage = GdUnitTestCaseBeforeStage.new()
var _stage_after :IGdUnitExecutionStage = GdUnitTestCaseAfterStage.new()
var _stage_test :IGdUnitExecutionStage = GdUnitTestCaseSingleTestStage.new()


func _execute(context :GdUnitExecutionContext) -> void:
	while context.retry_execution():
		var test_context := GdUnitExecutionContext.of(context)
		await _stage_before.execute(test_context)
		if not test_context.is_skipped():
			await _stage_test.execute(GdUnitExecutionContext.of(test_context))
		await _stage_after.execute(test_context)
		if test_context.is_success() or test_context.is_skipped() or test_context.is_interupted():
			break
	@warning_ignore("return_value_discarded")
	context.evaluate_test_retry_status()


func set_debug_mode(debug_mode :bool = false) -> void:
	super.set_debug_mode(debug_mode)
	_stage_before.set_debug_mode(debug_mode)
	_stage_after.set_debug_mode(debug_mode)
	_stage_test.set_debug_mode(debug_mode)
