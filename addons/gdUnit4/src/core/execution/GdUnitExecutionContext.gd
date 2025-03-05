## The execution context
## It contains all the necessary information about the executed stage, such as memory observers, reports, orphan monitor
class_name GdUnitExecutionContext

var _parent_context: GdUnitExecutionContext
var _sub_context: Array[GdUnitExecutionContext] = []
var _orphan_monitor: GdUnitOrphanNodesMonitor
var _memory_observer: GdUnitMemoryObserver
var _report_collector: GdUnitTestReportCollector
var _timer: LocalTime
var _test_case_name: StringName
var _test_case_parameter_set: Array
var _name: String
var _test_execution_iteration: int = 0
var _flaky_test_check := GdUnitSettings.is_test_flaky_check_enabled()
var _flaky_test_retries := GdUnitSettings.get_flaky_max_retries()


# execution states
var _is_calculated := false
var _is_success: bool
var _is_flaky: bool
var _is_skipped: bool
var _has_warnings: bool
var _has_failures: bool
var _has_errors: bool
var _failure_count := 0
var _orphan_count := 0
var _error_count := 0
var _skipped_count := 0


var error_monitor: GodotGdErrorMonitor = null:
	get:
		if _parent_context != null:
			return _parent_context.error_monitor
		if error_monitor == null:
			error_monitor = GodotGdErrorMonitor.new()
		return error_monitor


var test_suite: GdUnitTestSuite = null:
	get:
		if _parent_context != null:
			return _parent_context.test_suite
		return test_suite


var test_case: _TestCase = null:
	get:
		if test_case == null and _parent_context != null:
			return _parent_context.test_case
		return test_case


func _init(name: StringName, parent_context: GdUnitExecutionContext = null) -> void:
	_name = name
	_parent_context = parent_context
	_timer = LocalTime.now()
	_orphan_monitor = GdUnitOrphanNodesMonitor.new(name)
	_orphan_monitor.start()
	_memory_observer = GdUnitMemoryObserver.new()
	_report_collector = GdUnitTestReportCollector.new()
	if parent_context != null:
		parent_context._sub_context.append(self)


func dispose() -> void:
	_timer = null
	_orphan_monitor = null
	_report_collector = null
	_memory_observer = null
	_parent_context = null
	test_suite = null
	test_case = null
	dispose_sub_contexts()


func dispose_sub_contexts() -> void:
	for context in _sub_context:
		context.dispose()
	_sub_context.clear()


static func of(pe: GdUnitExecutionContext) -> GdUnitExecutionContext:
	var context := GdUnitExecutionContext.new(pe._test_case_name, pe)
	context._test_case_name = pe._test_case_name
	context._test_execution_iteration = pe._test_execution_iteration
	return context


static func of_test_suite(p_test_suite: GdUnitTestSuite) -> GdUnitExecutionContext:
	assert(p_test_suite, "test_suite is null")
	var context := GdUnitExecutionContext.new(p_test_suite.get_name())
	context.test_suite = p_test_suite
	return context


static func of_test_case(pe: GdUnitExecutionContext, p_test_case: _TestCase) -> GdUnitExecutionContext:
	assert(p_test_case, "test_case is null")
	var context := GdUnitExecutionContext.new(p_test_case.get_name(), pe)
	context.test_case = p_test_case
	return context


static func of_parameterized_test(pe: GdUnitExecutionContext, test_case_name: String, test_case_parameter_set: Array) -> GdUnitExecutionContext:
	var context := GdUnitExecutionContext.new(test_case_name, pe)
	context._test_case_name = test_case_name
	context._test_case_parameter_set = test_case_parameter_set
	return context


func get_test_suite_path() -> String:
	return test_suite.get_script().resource_path


func get_test_suite_name() -> StringName:
	return test_suite.get_name()


func get_test_case_name() -> StringName:
	if _test_case_name.is_empty():
		return test_case._test_case.display_name
	return _test_case_name


func error_monitor_start() -> void:
	error_monitor.start()


func error_monitor_stop() -> void:
	await error_monitor.scan()
	for error_report in error_monitor.to_reports():
		if error_report.is_error():
			_report_collector.push_back(error_report)


func orphan_monitor_start() -> void:
	_orphan_monitor.start()


func orphan_monitor_stop() -> void:
	_orphan_monitor.stop()


func add_report(report: GdUnitReport) -> void:
	_report_collector.push_back(report)


func reports() -> Array[GdUnitReport]:
	return _report_collector.reports()


func collect_reports(recursive: bool) -> Array[GdUnitReport]:
	if not recursive:
		return reports()
	var current_reports := reports()
	# we combine the reports of test_before(), test_after() and test() to be reported by `fire_test_ended`
	for sub_context in _sub_context:
		current_reports.append_array(sub_context.reports())
		# needs finally to clean the test reports to avoid counting twice
		sub_context.reports().clear()
	return current_reports


func collect_orphans(p_reports: Array[GdUnitReport]) -> int:
	var orphans := 0
	if not _sub_context.is_empty():
		orphans += collect_testcase_orphan_reports(_sub_context[0], p_reports)
	orphans += collect_teststage_orphan_reports(p_reports)
	return orphans


func collect_testcase_orphan_reports(context: GdUnitExecutionContext, p_reports: Array[GdUnitReport]) -> int:
	var orphans := context.count_orphans()
	if orphans > 0:
		p_reports.push_front(GdUnitReport.new()\
			.create(GdUnitReport.WARN, context.test_case.line_number(), GdAssertMessages.orphan_detected_on_test(orphans)))
	return orphans


func collect_teststage_orphan_reports(p_reports: Array[GdUnitReport]) -> int:
	var orphans := count_orphans()
	if orphans > 0:
		p_reports.push_front(GdUnitReport.new()\
			.create(GdUnitReport.WARN, test_case.line_number(), GdAssertMessages.orphan_detected_on_test_setup(orphans)))
	return orphans


func build_reports(recursive:= true) -> Array[GdUnitReport]:
	var collected_reports: Array[GdUnitReport] = collect_reports(recursive)
	if recursive:
		_orphan_count = collect_orphans(collected_reports)
	else:
		_orphan_count = count_orphans()
		if _orphan_count > 0:
			collected_reports.push_front(GdUnitReport.new() \
				.create(GdUnitReport.WARN, 1, GdAssertMessages.orphan_detected_on_suite_setup(_orphan_count)))
	_is_skipped = is_skipped()
	_skipped_count = count_skipped(recursive)
	_is_success = is_success()
	_is_flaky = is_flaky()
	_has_warnings = has_warnings()
	_has_errors = has_errors()
	_error_count = count_errors(recursive)
	if !_is_success:
		_has_failures = has_failures()
		_failure_count = count_failures(recursive)
	_is_calculated = true
	return collected_reports


# Evaluates the actual test case status by validate latest execution state (cold be more based on flaky max retry count)
func evaluate_test_retry_status() -> bool:
	# get latest test execution status
	var last_test_status :GdUnitExecutionContext = _sub_context.back()
	_is_skipped = last_test_status.is_skipped()
	_skipped_count = last_test_status.count_skipped(false)
	_is_success = last_test_status.is_success()
	# if success but it have more than one sub contexts the test was rerurn becouse of failures and will be marked as flaky
	_is_flaky = _is_success and _sub_context.size() > 1
	_has_warnings = last_test_status.has_warnings()
	_has_errors = last_test_status.has_errors()
	_error_count = last_test_status.count_errors(false)
	_has_failures = last_test_status.has_failures()
	_failure_count = last_test_status.count_failures(false)
	_orphan_count = last_test_status.collect_orphans(collect_reports(false))
	_is_calculated = true
	# finally cleanup the retry execution contexts
	dispose_sub_contexts()
	return _is_success


func get_execution_statistics() -> Dictionary:
	return {
		GdUnitEvent.RETRY_COUNT: _test_execution_iteration,
		GdUnitEvent.ORPHAN_NODES: _orphan_count,
		GdUnitEvent.ELAPSED_TIME: _timer.elapsed_since_ms(),
		GdUnitEvent.FAILED: !_is_success,
		GdUnitEvent.ERRORS: _has_errors,
		GdUnitEvent.WARNINGS: _has_warnings,
		GdUnitEvent.FLAKY: _is_flaky,
		GdUnitEvent.SKIPPED: _is_skipped,
		GdUnitEvent.FAILED_COUNT: _failure_count,
		GdUnitEvent.ERROR_COUNT: _error_count,
		GdUnitEvent.SKIPPED_COUNT: _skipped_count
	}


func has_failures() -> bool:
	return (
		_sub_context.any(func(c :GdUnitExecutionContext) -> bool:
			return c._has_failures if c._is_calculated else c.has_failures())
		or _report_collector.has_failures()
	)


func has_errors() -> bool:
	return (
		_sub_context.any(func(c :GdUnitExecutionContext) -> bool:
			return c._has_errors if c._is_calculated else c.has_errors())
		or _report_collector.has_errors()
	)


func has_warnings() -> bool:
	return (
		_sub_context.any(func(c :GdUnitExecutionContext) -> bool:
			return c._has_warnings if c._is_calculated else c.has_warnings())
		or _report_collector.has_warnings()
	)


func is_flaky() -> bool:
	return (
		_sub_context.any(func(c :GdUnitExecutionContext) -> bool:
			return c._is_flaky if c._is_calculated else c.is_flaky())
		or _test_execution_iteration > 1
	)


func is_success() -> bool:
	if _sub_context.is_empty():
		return not has_failures()

	var failed_context := _sub_context.filter(func(c :GdUnitExecutionContext) -> bool:
			return !(c._is_success if c._is_calculated else c.is_success()))
	return failed_context.is_empty() and not has_failures()


func is_skipped() -> bool:
	return (
		_sub_context.any(func(c :GdUnitExecutionContext) -> bool:
			return c._is_skipped if c._is_calculated else c.is_skipped())
		or test_case.is_skipped() if test_case != null else false
	)


func is_interupted() -> bool:
	return false if test_case == null else test_case.is_interupted()


func count_failures(recursive: bool) -> int:
	if not recursive:
		return _report_collector.count_failures()
	return _sub_context\
		.map(func(c :GdUnitExecutionContext) -> int:
				return c.count_failures(recursive)).reduce(sum, _report_collector.count_failures())


func count_errors(recursive: bool) -> int:
	if not recursive:
		return _report_collector.count_errors()
	return _sub_context\
		.map(func(c :GdUnitExecutionContext) -> int:
				return c.count_errors(recursive)).reduce(sum, _report_collector.count_errors())


func count_skipped(recursive: bool) -> int:
	if not recursive:
		return _report_collector.count_skipped()
	return _sub_context\
		.map(func(c :GdUnitExecutionContext) -> int:
				return c.count_skipped(recursive)).reduce(sum, _report_collector.count_skipped())


func count_orphans() -> int:
	var orphans := 0
	for c in _sub_context:
		orphans += c._orphan_monitor.orphan_nodes()
	return _orphan_monitor.orphan_nodes() - orphans


func sum(accum: int, number: int) -> int:
	return accum + number


func retry_execution() -> bool:
	var retry :=  _test_execution_iteration < 1 if not _flaky_test_check else _test_execution_iteration < _flaky_test_retries
	if retry:
		_test_execution_iteration += 1
	return retry


func register_auto_free(obj: Variant) -> Variant:
	return _memory_observer.register_auto_free(obj)


## Runs the gdunit garbage collector to free registered object
func gc() -> void:
	# unreference last used assert form the test to prevent memory leaks
	GdUnitThreadManager.get_current_context().clear_assert()
	await _memory_observer.gc()
	orphan_monitor_stop()
