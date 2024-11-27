## The parameterized test case execution stage.[br]
class_name GdUnitTestCaseParamaterizedTestStage
extends IGdUnitExecutionStage

var _stage_before: IGdUnitExecutionStage = GdUnitTestCaseBeforeStage.new()
var _stage_after: IGdUnitExecutionStage = GdUnitTestCaseAfterStage.new()
var _stage_test: IGdUnitExecutionStage = GdUnitTestCaseParameterSetTestStage.new()


## Executes a parameterized test case.[br]
## It executes synchronized following stages[br]
##  -> test_case( <test_parameters> ) [br]
func _execute(context: GdUnitExecutionContext) -> void:
	var test_case := context.test_case
	var test_parameter_index := test_case.test_parameter_index()
	var parameter_set_resolver := test_case.parameter_set_resolver()
	var test_names := parameter_set_resolver.build_test_case_names(test_case)

	# if all parameter sets has static values we can preload and reuse it for better performance
	var parameter_sets :Array = []
	if parameter_set_resolver.is_parameter_sets_static():
		parameter_sets = parameter_set_resolver.load_parameter_sets(test_case, true)

	for parameter_set_index in test_names.size():
		# is test_parameter_index is set, we run this parameterized test only
		if test_parameter_index != -1 and test_parameter_index != parameter_set_index:
			continue
		var current_test_case_name := test_names[parameter_set_index]
		var test_case_parameter_set: Array
		if parameter_set_resolver.is_parameter_set_static(parameter_set_index):
			test_case_parameter_set = parameter_sets[parameter_set_index]

		var test_context := GdUnitExecutionContext.of(context)
		test_context._test_case_name = current_test_case_name
		var has_errors := false
		while test_context.retry_execution():
			var retry_test_context := GdUnitExecutionContext.of(test_context)

			retry_test_context._test_case_name = current_test_case_name
			await _stage_before.execute(retry_test_context)
			if not test_case.is_interupted():
				# we need to load paramater set at execution level after the before stage to get the actual variables from the current test
				if not parameter_set_resolver.is_parameter_set_static(parameter_set_index):
					test_case_parameter_set = _load_parameter_set(context, parameter_set_index)
				await _stage_test.execute(GdUnitExecutionContext.of_parameterized_test(retry_test_context, current_test_case_name, test_case_parameter_set))
			await _stage_after.execute(retry_test_context)
			has_errors = retry_test_context.has_errors()
			if retry_test_context.is_success() or retry_test_context.is_skipped() or retry_test_context.is_interupted():
				break

		var is_success := test_context.evaluate_test_retry_status()
		report_test_failure(context, !is_success, has_errors, parameter_set_index)

		if test_case.is_interupted():
			break
	await context.gc()


func report_test_failure(test_context: GdUnitExecutionContext, is_failed: bool, has_errors: bool, parameter_set_index: int) -> void:
	var test_case := test_context.test_case

	if is_failed:
		test_context.add_report(GdUnitReport.new().create(GdUnitReport.FAILURE, test_case.line_number(), "Test failed at parameterized index %d." % parameter_set_index))
	if has_errors:
		test_context.add_report(GdUnitReport.new().create(GdUnitReport.ABORT, test_case.line_number(), "Test aborted at parameterized index %d." % parameter_set_index))


func _load_parameter_set(context: GdUnitExecutionContext, parameter_set_index: int) -> Array:
	var test_case := context.test_case
	# we need to exchange temporary for parameter resolving the execution context
	# this is necessary because of possible usage of `auto_free` and needs to run in the parent execution context
	var thread_context := GdUnitThreadManager.get_current_context()
	var save_execution_context := thread_context.get_execution_context()
	thread_context.set_execution_context(context)
	var parameters := test_case.load_parameter_sets()
	# restore the original execution context and restart the orphan monitor to get new instances into account
	thread_context.set_execution_context(save_execution_context)
	save_execution_context.orphan_monitor_start()
	return parameters[parameter_set_index]


func set_debug_mode(debug_mode: bool=false) -> void:
	super.set_debug_mode(debug_mode)
	_stage_before.set_debug_mode(debug_mode)
	_stage_after.set_debug_mode(debug_mode)
	_stage_test.set_debug_mode(debug_mode)
