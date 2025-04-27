class_name GdUnitTestCaseReport
extends GdUnitReportSummary

var _suite_name :String
var _failure_reports :Array[GdUnitReport]


@warning_ignore("shadowed_variable")
func _init(p_resource_path: String, p_suite_name: String, p_test_name: String) -> void:
	_resource_path = p_resource_path
	_suite_name = p_suite_name
	_name = p_test_name


func suite_name() -> String:
	return _suite_name


func failure_report() -> String:
	var html_report := ""
	for report in get_test_reports():
		html_report += convert_rtf_to_html(str(report))
	return html_report


func create_record(_report_dir :String) -> String:
	return GdUnitHtmlPatterns.TABLE_RECORD_TESTCASE\
		.replace(GdUnitHtmlPatterns.REPORT_STATE, report_state().to_lower())\
		.replace(GdUnitHtmlPatterns.REPORT_STATE_LABEL, report_state())\
		.replace(GdUnitHtmlPatterns.TESTCASE_NAME, name())\
		.replace(GdUnitHtmlPatterns.SKIPPED_COUNT, str(skipped_count()))\
		.replace(GdUnitHtmlPatterns.ORPHAN_COUNT, str(orphan_count()))\
		.replace(GdUnitHtmlPatterns.DURATION, LocalTime.elapsed(_duration))\
		.replace(GdUnitHtmlPatterns.FAILURE_REPORT, failure_report())


func add_testcase_reports(reports: Array[GdUnitReport]) -> void:
	_failure_reports.append_array(reports)


func set_testcase_counters(p_error_count: int, p_failure_count: int, p_orphan_count: int,
	p_is_skipped: bool, p_is_flaky: bool, p_duration: int) -> void:
	_error_count = p_error_count
	_failure_count = p_failure_count
	_orphan_count = p_orphan_count
	_skipped_count = p_is_skipped
	_flaky_count = p_is_flaky as int
	_duration = p_duration


func get_test_reports() -> Array[GdUnitReport]:
	return _failure_reports
