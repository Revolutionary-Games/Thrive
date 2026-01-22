class_name GdUnitByPathReport
extends GdUnitReportSummary


func _init(p_path :String, report_summaries :Array[GdUnitReportSummary]) -> void:
	_resource_path = p_path
	_reports = report_summaries


# -> Dictionary[String, Array[GdUnitReportSummary]]
static func sort_reports_by_path(report_summaries :Array[GdUnitReportSummary]) -> Dictionary:
	var by_path := Dictionary()
	for report in report_summaries:
		var suite_path :String = ProjectSettings.localize_path(report.path())
		var suite_report :Array[GdUnitReportSummary] = by_path.get(suite_path, [] as Array[GdUnitReportSummary])
		suite_report.append(report)
		by_path[suite_path] = suite_report
	return by_path


func path() -> String:
	return _resource_path.replace("res://", "").trim_suffix("/")


func create_record(report_link :String) -> String:
	return GdUnitHtmlPatterns.build(GdUnitHtmlPatterns.TABLE_RECORD_PATH, self, report_link)


func write(report_dir :String) -> String:
	calculate_summary()
	var template := GdUnitHtmlPatterns.load_template("res://addons/gdUnit4/src/reporters/html/template/folder_report.html")
	var path_report := GdUnitHtmlPatterns.build(template, self, "")
	path_report = apply_testsuite_reports(report_dir, path_report, _reports)

	var output_path := "%s/path/%s.html" % [report_dir, path().replace("/", ".")]
	var dir := output_path.get_base_dir()
	if not DirAccess.dir_exists_absolute(dir):
		@warning_ignore("return_value_discarded")
		DirAccess.make_dir_recursive_absolute(dir)
	FileAccess.open(output_path, FileAccess.WRITE).store_string(path_report)
	return output_path


func apply_testsuite_reports(report_dir :String, template :String, test_suite_reports :Array[GdUnitReportSummary]) -> String:
	var table_records := PackedStringArray()
	for report:GdUnitTestSuiteReport in test_suite_reports:
		var report_link := GdUnitHtmlReportWriter.create_output_path(report_dir, report.path(), report.name()).replace(report_dir, "..")
		@warning_ignore("return_value_discarded")
		table_records.append(GdUnitHtmlPatterns.create_suite_record(report_link, report))
	return template.replace(GdUnitHtmlPatterns.TABLE_BY_TESTSUITES, "\n".join(table_records))


func calculate_summary() -> void:
	for report:GdUnitTestSuiteReport in get_reports():
		_error_count += report.error_count()
		_failure_count += report.failure_count()
		_orphan_count += report.orphan_count()
		_skipped_count += report.skipped_count()
		_flaky_count += report.flaky_count()
		_duration += report.duration()
