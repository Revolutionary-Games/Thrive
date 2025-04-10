class_name GdUnitReportSummary
extends RefCounted

const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")

const CHARACTERS_TO_ENCODE := {
	'<' : '&lt;',
	'>' : '&gt;'
}

var _resource_path :String
var _name :String
var _test_count := 0
var _failure_count := 0
var _error_count := 0
var _orphan_count := 0
var _skipped_count := 0
var _flaky_count := 0
var _duration := 0
var _reports :Array[GdUnitReportSummary] = []

func name() -> String:
	return _name


func name_html_encoded() -> String:
	return html_encode(_name)


func path() -> String:
	return _resource_path.get_base_dir().replace("res://", "")


func get_resource_path() -> String:
	return _resource_path


func suite_count() -> int:
	return _reports.size()


func suite_executed_count() -> int:
	var executed := _reports.size()
	for report in _reports:
		if report.test_count() == report.skipped_count():
			executed -= 1
	return executed


func test_count() -> int:
	var count := _test_count
	for report in _reports:
		count += report.test_count()
	return count


func test_executed_count() -> int:
	return test_count() - skipped_count()


func success_count() -> int:
	return test_count() - error_count() - failure_count() - flaky_count() - skipped_count()


func error_count() -> int:
	return _error_count


func failure_count() -> int:
	return _failure_count


func skipped_count() -> int:
	return _skipped_count


func flaky_count() -> int:
	return _flaky_count


func orphan_count() -> int:
	return _orphan_count


func duration() -> int:
	return _duration


func get_reports() -> Array:
	return _reports


func add_report(report :GdUnitReportSummary) -> void:
	_reports.append(report)


func report_state() -> String:
	return calculate_state(error_count(), failure_count(), orphan_count(), flaky_count(), skipped_count())


func succes_rate() -> String:
	return calculate_succes_rate(test_count(), error_count(), failure_count())


func calculate_state(p_error_count :int, p_failure_count :int, p_orphan_count :int, p_flaky_count: int, p_skipped_count: int) -> String:
	if p_error_count > 0:
		return "ERROR"
	if p_failure_count > 0:
		return "FAILED"
	if p_flaky_count > 0:
		return "FLAKY"
	if p_orphan_count > 0:
		return "WARNING"
	if p_skipped_count > 0:
		return "SKIPPED"
	return "PASSED"


func calculate_succes_rate(p_test_count :int, p_error_count :int, p_failure_count :int) -> String:
	if p_failure_count == 0:
		return "100%"
	var count := p_test_count-p_failure_count-p_error_count
	if count < 0:
		return "0%"
	return "%d" % (( 0 if count < 0 else count) * 100.0 / p_test_count) + "%"


func create_summary(_report_dir :String) -> String:
	return ""


func html_encode(value: String) -> String:
	for key: String in CHARACTERS_TO_ENCODE.keys():
		@warning_ignore("unsafe_cast")
		value = value.replace(key, CHARACTERS_TO_ENCODE[key] as String)
	return value


func convert_rtf_to_html(bbcode :String) -> String:
	return GdUnitTools.richtext_normalize(bbcode)
