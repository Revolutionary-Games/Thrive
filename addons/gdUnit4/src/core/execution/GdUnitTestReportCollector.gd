# Collects all reports seperated as warnings, failures and errors
class_name GdUnitTestReportCollector
extends RefCounted


var _reports :Array[GdUnitReport] = []


static func __filter_is_error(report :GdUnitReport) -> bool:
	return report.is_error()


static func __filter_is_failure(report :GdUnitReport) -> bool:
	return report.is_failure()


static func __filter_is_warning(report :GdUnitReport) -> bool:
	return report.is_warning()


static func __filter_is_skipped(report :GdUnitReport) -> bool:
	return report.is_skipped()


func count_failures() -> int:
	return _reports.filter(__filter_is_failure).size()


func count_errors() -> int:
	return _reports.filter(__filter_is_error).size()


func count_warnings() -> int:
	return _reports.filter(__filter_is_warning).size()


func count_skipped() -> int:
	return _reports.filter(__filter_is_skipped).size()


func has_failures() -> bool:
	return _reports.any(__filter_is_failure)


func has_errors() -> bool:
	return _reports.any(__filter_is_error)


func has_warnings() -> bool:
	return _reports.any(__filter_is_warning)


func has_skipped() -> bool:
	return _reports.any(__filter_is_skipped)


func reports() -> Array[GdUnitReport]:
	return _reports


func push_back(report :GdUnitReport) -> void:
	_reports.push_back(report)
