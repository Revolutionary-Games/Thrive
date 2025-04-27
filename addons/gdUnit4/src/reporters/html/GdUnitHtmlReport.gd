class_name GdUnitHtmlReport
extends GdUnitReportSummary

const REPORT_DIR_PREFIX = "report_"

var _report_path: String
var _iteration: int
var _max_reports: int


func _init(report_path :String, max_reports: int) -> void:
	_max_reports = max_reports
	if max_reports > 1:
		_iteration = GdUnitFileAccess.find_last_path_index(report_path, REPORT_DIR_PREFIX) + 1
	else:
		_iteration = 1
	_report_path = "%s/%s%d" % [report_path, REPORT_DIR_PREFIX, _iteration]
	@warning_ignore("return_value_discarded")
	DirAccess.make_dir_recursive_absolute(_report_path)


func add_testsuite_report(p_resource_path: String, p_suite_name: String, p_test_count: int) -> void:
	_reports.append(GdUnitTestSuiteReport.new(p_resource_path, p_suite_name, p_test_count))


@warning_ignore("shadowed_variable")
func add_testcase(resource_path :String, suite_name :String, test_name: String) -> void:
	for report:GdUnitTestSuiteReport in _reports:
		if report.get_resource_path() == resource_path:
			var test_report := GdUnitTestCaseReport.new(resource_path, suite_name, test_name)
			report.add_or_create_test_report(test_report)


func add_testsuite_reports(
	p_resource_path :String,
	p_reports :Array = []) -> void:

	for report:GdUnitTestSuiteReport in _reports:
		if report.get_resource_path() == p_resource_path:
			report.set_reports(p_reports)


func add_testcase_reports(
	p_resource_path: String,
	p_test_name: String,
	p_reports: Array[GdUnitReport]) -> void:

	for report:GdUnitTestSuiteReport in _reports:
		if report.get_resource_path() == p_resource_path:
			report.add_testcase_reports(p_test_name, p_reports)


func update_testsuite_counters(
	p_resource_path :String,
	p_error_count: int,
	p_failure_count: int,
	p_orphan_count: int,
	p_skipped_count: int,
	p_flaky_count: int,
	p_duration: int) -> void:

	for report:GdUnitTestSuiteReport in _reports:
		if report.get_resource_path() == p_resource_path:
			report.update_testsuite_counters(p_error_count, p_failure_count, p_orphan_count, p_skipped_count, p_flaky_count, p_duration)
	update_summary_counters(p_error_count, p_failure_count, p_orphan_count, p_skipped_count, p_flaky_count, 0)


func set_testcase_counters(
	p_resource_path: String,
	p_test_name: String,
	p_error_count: int,
	p_failure_count: int,
	p_orphan_count: int,
	p_is_skipped: bool,
	p_is_flaky: bool,
	p_duration: int) -> void:

	for report:GdUnitTestSuiteReport in _reports:
		if report.get_resource_path() == p_resource_path:
			report.set_testcase_counters(p_test_name, p_error_count, p_failure_count, p_orphan_count,
				p_is_skipped, p_is_flaky, p_duration)


func update_summary_counters(
	p_error_count: int,
	p_failure_count: int,
	p_orphan_count: int,
	p_skipped_count: int,
	p_flaky_count: int,
	p_duration: int) -> void:

	_error_count += p_error_count
	_failure_count += p_failure_count
	_orphan_count += p_orphan_count
	_skipped_count += p_skipped_count
	_flaky_count += p_flaky_count
	_duration += p_duration


func write() -> void:
	var template := GdUnitHtmlPatterns.load_template("res://addons/gdUnit4/src/reporters/html/template/index.html")
	var to_write := GdUnitHtmlPatterns.build(template, self, "")
	to_write = apply_path_reports(_report_path, to_write, _reports)
	to_write = apply_testsuite_reports(_report_path, to_write, _reports)
	# write report
	FileAccess.open(report_file(), FileAccess.WRITE).store_string(to_write)
	@warning_ignore("return_value_discarded")
	GdUnitFileAccess.copy_directory("res://addons/gdUnit4/src/reporters/html/template/css/", _report_path + "/css")


func report_file() -> String:
	return "%s/index.html" % _report_path


func delete_history() -> int:
	return GdUnitFileAccess.delete_path_index_lower_equals_than(_report_path.get_base_dir(), REPORT_DIR_PREFIX, _iteration-_max_reports)


func apply_path_reports(report_dir :String, template :String, report_summaries :Array) -> String:
	#Dictionary[String, Array[GdUnitReportSummary]]
	var path_report_mapping := GdUnitByPathReport.sort_reports_by_path(report_summaries)
	var table_records := PackedStringArray()
	var paths :Array[String] = []
	paths.append_array(path_report_mapping.keys())
	paths.sort()
	for report_path in paths:
		var reports: Array[GdUnitReportSummary] = path_report_mapping.get(report_path)
		var report := GdUnitByPathReport.new(report_path, reports)
		var report_link :String = report.write(report_dir).replace(report_dir, ".")
		@warning_ignore("return_value_discarded")
		table_records.append(report.create_record(report_link))
	return template.replace(GdUnitHtmlPatterns.TABLE_BY_PATHS, "\n".join(table_records))


func apply_testsuite_reports(report_dir: String, template: String, test_suite_reports: Array[GdUnitReportSummary]) -> String:
	var table_records := PackedStringArray()
	for report: GdUnitTestSuiteReport in test_suite_reports:
		var report_link :String = report.write(report_dir).replace(report_dir, ".")
		@warning_ignore("return_value_discarded")
		table_records.append(report.create_record(report_link) as String)
	return template.replace(GdUnitHtmlPatterns.TABLE_BY_TESTSUITES, "\n".join(table_records))


func iteration() -> int:
	return _iteration
