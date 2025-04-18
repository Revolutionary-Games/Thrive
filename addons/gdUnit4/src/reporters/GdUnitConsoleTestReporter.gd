@tool
class_name GdUnitConsoleTestReporter
extends GdUnitTestReporter

const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")

var _writer: GdUnitMessageWritter
var _status_indent := 86
var _detailed: bool
var _text_color: Color = Color.ANTIQUE_WHITE
var _function_color: Color = Color.ANTIQUE_WHITE
var _engine_type_color: Color = Color.ANTIQUE_WHITE



func _init(writer: GdUnitMessageWritter, detailed := false) -> void:
	_writer = writer
	_writer.clear()
	_detailed = detailed
	if _detailed:
		_status_indent = 20
	init_colors()


func init_colors() -> void:
	if Engine.is_editor_hint():
		var settings := EditorInterface.get_editor_settings()
		_text_color = settings.get_setting("text_editor/theme/highlighting/text_color")
		_function_color = settings.get_setting("text_editor/theme/highlighting/function_color")
		_engine_type_color = settings.get_setting("text_editor/theme/highlighting/engine_type_color")


func clear() -> void:
	_writer.clear()


func on_gdunit_event(event: GdUnitEvent) -> void:
	match event.type():
		GdUnitEvent.INIT:
			init_summary()

		GdUnitEvent.STOP:
			_print_summary()
			println_message(build_executed_test_suite_msg(processed_suite_count(), processed_suite_count()), Color.DARK_SALMON)
			println_message(build_executed_test_case_msg(total_test_count(), total_flaky_count()), Color.DARK_SALMON)
			println_message("Total execution time: %s" % LocalTime.elapsed(elapsed_time()), Color.DARK_SALMON)
			# We need finally to set the wave effect to enable the animations
			_writer.effect(GdUnitMessageWritter.Effect.WAVE).print_at("", 0)

		GdUnitEvent.TESTSUITE_BEFORE:
			init_statistics()
			print_message("Run Test Suite: ", Color.DARK_TURQUOISE)
			println_message(event.resource_path(), _engine_type_color)

		GdUnitEvent.TESTSUITE_AFTER:
			if not event.reports().is_empty():
				_writer.color(Color.DARK_SALMON)\
					.style(GdUnitMessageWritter.BOLD)\
					.println_message(event.suite_name()+":finalze")
			_print_failure_report(event.reports())
			_print_statistics(build_test_suite_statisitcs(event))
			_print_status(event)
			println_message("")
			if _detailed:
				println_message("")

		GdUnitEvent.TESTCASE_BEFORE:
			var test := find_test_by_id(event.guid())
			_print_test_path(test, event.guid())
			if _detailed:
				_writer.color(Color.FOREST_GREEN).print_at("STARTED", _status_indent)
				println_message("")

		GdUnitEvent.TESTCASE_AFTER:
			var test := find_test_by_id(event.guid())
			update_statistics(event)
			if _detailed:
				_print_test_path(test, event.guid())
			_print_status(event)
			_print_failure_report(event.reports())
			if _detailed:
				println_message("")


func _print_test_path(test: GdUnitTestCase, uid: GdUnitGUID) -> void:
	if test == null:
		prints_warning("Can't print full test info, the test by uid: '%s' was not discovered." % uid)
		_writer.indent(1).color(_engine_type_color).print_message("Test ID: %s" % uid)
		return

	var suite_name := test.source_file if _detailed else test.suite_name
	_writer.indent(1).color(_engine_type_color).print_message(suite_name)
	print_message(" > ")
	print_message(test.display_name, _function_color)


func _print_status(event: GdUnitEvent) -> void:
	if event.is_flaky() and event.is_success():
		var retries: int = event.statistic(GdUnitEvent.RETRY_COUNT)
		_writer.color(Color.GREEN_YELLOW)\
			.style(GdUnitMessageWritter.ITALIC)\
			.print_at("FLAKY (%d retries)" % retries, _status_indent)
	elif event.is_success():
		_writer.color(Color.FOREST_GREEN).print_at("PASSED", _status_indent)
	elif event.is_skipped():
		_writer.color(Color.GOLDENROD).style(GdUnitMessageWritter.ITALIC).print_at("SKIPPED", _status_indent)
	elif event.is_failed() or event.is_error():
		var retries :int = event.statistic(GdUnitEvent.RETRY_COUNT)
		var message := "FAILED (retry %d)" % retries if retries > 1 else "FAILED"
		_writer.color(Color.FIREBRICK)\
			.style(GdUnitMessageWritter.BOLD)\
			.effect(GdUnitMessageWritter.Effect.WAVE)\
			.print_at(message, _status_indent)
	elif event.is_warning():
		_writer.color(Color.GOLDENROD)\
			.style(GdUnitMessageWritter.UNDERLINE)\
			.print_at("WARNING", _status_indent)

	println_message(" %s" % LocalTime.elapsed(event.elapsed_time()), Color.CORNFLOWER_BLUE)


func _print_failure_report(reports: Array[GdUnitReport]) -> void:
	for report in reports:
		if (
			report.is_failure()
			or report.is_error()
			or report.is_warning()
			or report.is_skipped()
		):
			_writer.indent(1)\
				.color(Color.DARK_TURQUOISE)\
				.style(GdUnitMessageWritter.BOLD | GdUnitMessageWritter.UNDERLINE)\
				.println_message("Report:")
			var text := GdUnitTools.richtext_normalize(str(report))
			for line in text.split("\n", false):
				_writer.indent(2).color(Color.DARK_TURQUOISE).println_message(line)

	if not reports.is_empty():
		println_message("")


func _print_statistics(statistics: Dictionary) -> void:
	print_message("Statistics:", Color.DODGER_BLUE)
	print_message(" %d tests cases | %d errors | %d failures | %d flaky | %d skipped | %d orphans |" %\
		[statistics["total_count"],
		statistics["error_count"],
		statistics["failed_count"],
		statistics["flaky_count"],
		statistics["skipped_count"],
		statistics["orphan_nodes"]])


func _print_summary() -> void:
	print_message("Overall Summary:", Color.DODGER_BLUE)
	_writer\
		.println_message(" %d tests cases | %d errors | %d failures | %d flaky | %d skipped | %d orphans |" % [
			_summary["total_count"],
			_summary["error_count"],
			_summary["failed_count"],
			_summary["flaky_count"],
			_summary["skipped_count"],
			_summary["orphan_nodes"]
		])


func build_executed_test_suite_msg(executed_count: int, total_count: int) -> String:
	if executed_count == total_count:
		return "Executed test suites: (%d/%d)" % [executed_count, total_count]
	return "Executed test suites: (%d/%d), %d skipped" % [executed_count, total_count, (total_count - executed_count)]


func build_executed_test_case_msg(total_count: int, skipped_count: int) -> String:
	if skipped_count == 0:
		return "Executed test cases : (%d/%d)" % [total_count, total_count]
	return "Executed test cases : (%d/%d), %d skipped" % [total_count-skipped_count, total_count, skipped_count]


func print_message(message: String, color: Color = _text_color) -> void:
	_writer.color(color).print_message(message)


func println_message(message: String, color: Color = _text_color) -> void:
	_writer.color(color).println_message(message)


func prints_warning(message: String) -> void:
	_writer.prints_warning(message)


func prints_error(message: String) -> void:
	_writer.prints_error(message)
