class_name GdUnitTestReporter
extends RefCounted

var _guard := GdUnitTestDiscoverGuard.instance()
var _statistics := {}
var _summary := {}


func on_gdunit_event(_event: GdUnitEvent) -> void:
	push_error("Reporter: 'on_gdunit_event' is not implemented!")


func init_summary() -> void:
	_summary["suite_count"] = 0
	_summary["total_count"] = 0
	_summary["error_count"] = 0
	_summary["failed_count"] = 0
	_summary["skipped_count"] = 0
	_summary["flaky_count"] = 0
	_summary["orphan_nodes"] = 0
	_summary["elapsed_time"] = 0


func init_statistics() -> void:
	_statistics.clear()


func update_statistics(event: GdUnitEvent) -> void:
	var test_statisitics: Dictionary = _statistics.get_or_add(event.guid(), {
		"error_count" : 0,
		"failed_count" : 0,
		"skipped_count" : event.is_skipped() as int,
		"flaky_count" : 0,
		"orphan_nodes" : 0
	})
	test_statisitics["error_count"] = event.is_error() as int
	test_statisitics["failed_count"] = event.is_failed() as int
	test_statisitics["flaky_count"] = event.is_flaky() as int
	test_statisitics["orphan_nodes"] = event.orphan_nodes()


func build_test_suite_statisitcs(event: GdUnitEvent) -> Dictionary:
	var statistic :=  {
		"total_count" : _statistics.size(),
		"error_count" : event.error_count(),
		"failed_count" : event.failed_count(),
		"skipped_count" : event.skipped_count(),
		"flaky_count" : 0,
		"orphan_nodes" : event.orphan_nodes()
	}
	_summary["suite_count"] += 1
	_summary["total_count"] += _statistics.size()
	# Add the suite hook specific counters
	_summary["error_count"] +=  event.error_count()
	_summary["failed_count"] +=  event.failed_count()
	_summary["orphan_nodes"] +=  event.orphan_nodes()
	_summary["elapsed_time"] += event.elapsed_time()

	for key: String in ["error_count", "failed_count", "skipped_count", "flaky_count", "orphan_nodes"]:
		var value: int = _statistics.values().reduce(get_value.bind(key), 0 )
		statistic[key] = value
		_summary[key] += value

	return statistic


func get_value(acc: int, value: Dictionary, key: String) -> int:
	return acc + value[key]


func find_test_by_id(id: GdUnitGUID) -> GdUnitTestCase:
	return _guard.find_test_by_id(id)


func processed_suite_count() -> int:
	return _summary["suite_count"]


func total_test_count() -> int:
	return _summary["total_count"]


func total_flaky_count() -> int:
	return _summary["flaky_count"]


func total_error_count() -> int:
	return _summary["error_count"]


func total_failure_count() -> int:
	return _summary["failed_count"]


func total_skipped_count() -> int:
	return _summary["skipped_count"]


func total_orphan_count() -> int:
	return _summary["orphan_nodes"]


func elapsed_time() -> int:
	return _summary["elapsed_time"]
