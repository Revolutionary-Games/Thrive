class_name GdUnitHtmlPatterns
extends RefCounted

const TABLE_RECORD_TESTSUITE = """
								<tr class="${report_state}">
									<td><a href=${report_link}>${testsuite_name}</a></td>
									<td><span class="status status-${report_state}">${report_state_label}</span></td>
									<td>${test_count}</td>
									<td>${skipped_count}</td>
									<td>${flaky_count}</td>
									<td>${failure_count}</td>
									<td>${orphan_count}</td>
									<td>${duration}</td>
									<td>
										<div class="status-bar">
											<div class="status-bar-column status-skipped" style="width: ${skipped-percent};"></div>
											<div class="status-bar-column status-passed" style="width: ${passed-percent};"></div>
											<div class="status-bar-column status-flaky" style="width: ${flaky-percent};"></div>
											<div class="status-bar-column status-error" style="width: ${error-percent};"></div>
											<div class="status-bar-column status-failed" style="width: ${failed-percent};"></div>
											<div class="status-bar-column status-warning" style="width: ${warning-percent};"></div>
										</div>
									</td>
								</tr>
"""

const TABLE_RECORD_PATH = """
								<tr class="${report_state}">
									<td><a class="${report_state}" href="${report_link}">${path}</a></td>
									<td><span class="status status-${report_state}">${report_state_label}</span></td>
									<td>${test_count}</td>
									<td>${skipped_count}</td>
									<td>${flaky_count}</td>
									<td>${failure_count}</td>
									<td>${orphan_count}</td>
									<td>${duration}</td>
									<td>
										<div class="status-bar">
											<div class="status-bar-column status-skipped" style="width: ${skipped-percent};"></div>
											<div class="status-bar-column status-passed" style="width: ${passed-percent};"></div>
											<div class="status-bar-column status-flaky" style="width: ${flaky-percent};"></div>
											<div class="status-bar-column status-error" style="width: ${error-percent};"></div>
											<div class="status-bar-column status-failed" style="width: ${failed-percent};"></div>
											<div class="status-bar-column status-warning" style="width: ${warning-percent};"></div>
										</div>
									</td>
								</tr>
"""


const TABLE_REPORT_TESTSUITE = """
								<tr class="${report_state}">
									<td>TestSuite hooks</td>
									<td>n/a</td>
									<td>${orphan_count}</td>
									<td>${duration}</td>
									<td class="report-column">
										<pre>
${failure-report}
										</pre>
									</td>
								</tr>
"""


const TABLE_RECORD_TESTCASE = """
								<tr class="testcase-group">
									<td>${testcase_name}</td>
									<td><span class="status status-${report_state}">${report_state_label}</span></td>
									<td>${skipped_count}</td>
									<td>${orphan_count}</td>
									<td>${duration}</td>
									<td class="report-column">
										<pre>
${failure-report}
										</pre>
									</td>
								</tr>
"""

const TABLE_BY_PATHS = "${report_table_paths}"
const TABLE_BY_TESTSUITES = "${report_table_testsuites}"
const TABLE_BY_TESTCASES = "${report_table_tests}"

# the report state success, error, warning
const REPORT_STATE = "${report_state}"
const REPORT_STATE_LABEL = "${report_state_label}"
const PATH = "${path}"
const RESOURCE_PATH = "${resource_path}"
const TESTSUITE_COUNT = "${suite_count}"
const TESTCASE_COUNT = "${test_count}"
const FAILURE_COUNT = "${failure_count}"
const FLAKY_COUNT = "${flaky_count}"
const SKIPPED_COUNT = "${skipped_count}"
const ORPHAN_COUNT = "${orphan_count}"
const DURATION = "${duration}"
const FAILURE_REPORT = "${failure-report}"
const SUCCESS_PERCENT = "${success_percent}"


const QUICK_STATE_SKIPPED = "${skipped-percent}"
const QUICK_STATE_PASSED = "${passed-percent}"
const QUICK_STATE_FLAKY = "${flaky-percent}"
const QUICK_STATE_ERROR = "${error-percent}"
const QUICK_STATE_FAILED = "${failed-percent}"
const QUICK_STATE_WARNING = "${warning-percent}"

const TESTSUITE_NAME = "${testsuite_name}"
const TESTCASE_NAME = "${testcase_name}"
const REPORT_LINK = "${report_link}"
const BREADCRUMP_PATH_LINK = "${breadcrumb_path_link}"
const BUILD_DATE = "${buid_date}"


static func current_date() -> String:
	return Time.get_datetime_string_from_system(true, true)


static func build(template: String, report: GdUnitReportSummary, report_link: String) -> String:
	return template\
		.replace(PATH, get_report_path(report))\
		.replace(BREADCRUMP_PATH_LINK, get_path_as_link(report))\
		.replace(RESOURCE_PATH, report.get_resource_path())\
		.replace(TESTSUITE_NAME, report.name_html_encoded())\
		.replace(TESTSUITE_COUNT, str(report.suite_count()))\
		.replace(TESTCASE_COUNT, str(report.test_count()))\
		.replace(FAILURE_COUNT, str(report.error_count() + report.failure_count()))\
		.replace(FLAKY_COUNT, str(report.flaky_count()))\
		.replace(SKIPPED_COUNT, str(report.skipped_count()))\
		.replace(ORPHAN_COUNT, str(report.orphan_count()))\
		.replace(DURATION, LocalTime.elapsed(report.duration()))\
		.replace(SUCCESS_PERCENT, report.calculate_succes_rate(report.test_count(), report.error_count(), report.failure_count()))\
		.replace(REPORT_STATE, report.report_state().to_lower())\
		.replace(REPORT_STATE_LABEL, report.report_state())\
		.replace(QUICK_STATE_SKIPPED, calculate_percentage(report.test_count(), report.skipped_count()))\
		.replace(QUICK_STATE_PASSED, calculate_percentage(report.test_count(), report.success_count()))\
		.replace(QUICK_STATE_FLAKY, calculate_percentage(report.test_count(), report.flaky_count()))\
		.replace(QUICK_STATE_ERROR, calculate_percentage(report.test_count(), report.error_count()))\
		.replace(QUICK_STATE_FAILED, calculate_percentage(report.test_count(), report.failure_count()))\
		.replace(QUICK_STATE_WARNING, calculate_percentage(report.test_count(), 0))\
		.replace(REPORT_LINK, report_link)\
		.replace(BUILD_DATE, current_date())


static func load_template(template_name :String) -> String:
	return FileAccess.open(template_name, FileAccess.READ).get_as_text()


static func get_path_as_link(report: GdUnitReportSummary) -> String:
	return "../path/%s.html" % report.path().replace("/", ".")


static func get_report_path(report: GdUnitReportSummary) -> String:
	var path := report.path()
	if path.is_empty():
		return "/"
	return path


static func calculate_percentage(p_test_count: int, count: int) -> String:
	if count <= 0:
		return "0%"
	return "%d" % (( 0 if count < 0 else count) * 100.0 / p_test_count) + "%"
