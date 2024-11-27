## The test suite shutdown hook implementation.[br]
## It executes the 'after()' block from the test-suite.
class_name GdUnitTestSuiteAfterStage
extends IGdUnitExecutionStage


const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")


func _execute(context :GdUnitExecutionContext) -> void:
	var test_suite := context.test_suite

	@warning_ignore("redundant_await")
	await test_suite.after()
	await context.gc()
	var reports := context.build_reports(false)
	fire_event(GdUnitEvent.new()\
		.suite_after(context.get_test_suite_path(),\
			test_suite.get_name(),
			context.get_execution_statistics(),
			reports))

	GdUnitFileAccess.clear_tmp()
	# Guard that checks if all doubled (spy/mock) objects are released
	GdUnitClassDoubler.check_leaked_instances()
	# we hide the scene/main window after runner is finished
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MINIMIZED)
