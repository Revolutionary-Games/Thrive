class_name GdUnitTestCaseParameterSetTestStage
extends IGdUnitExecutionStage


## Executes a parameterized test case 'test_<name>()' by given parameters.[br]
## It executes synchronized following stages[br]
##  -> test_case() [br]
func _execute(context: GdUnitExecutionContext) -> void:
	await context.test_case.execute_paramaterized(context._test_case_parameter_set)
	await context.gc()
