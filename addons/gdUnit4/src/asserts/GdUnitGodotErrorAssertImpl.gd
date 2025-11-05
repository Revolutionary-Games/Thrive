extends GdUnitGodotErrorAssert

var _current_failure_message := ""
var _custom_failure_message := ""
var _additional_failure_message := ""
var _callable: Callable


func _init(callable: Callable) -> void:
	# save the actual assert instance on the current thread context
	GdUnitThreadManager.get_current_context().set_assert(self)
	GdAssertReports.reset_last_error_line_number()
	_callable = callable


func _execute() -> Array[ErrorLogEntry]:
	# execute the given code and monitor for runtime errors
	if _callable == null or not _callable.is_valid():
		@warning_ignore("return_value_discarded")
		_report_error("Invalid Callable '%s'" % _callable)
	else:
		await _callable.call()
	return await _error_monitor().scan(true)


func _error_monitor() -> GodotGdErrorMonitor:
	return GdUnitThreadManager.get_current_context().get_execution_context().error_monitor


func failure_message() -> String:
	return _current_failure_message


func _report_success() -> GdUnitAssert:
	GdAssertReports.report_success()
	return self


func _report_error(error_message: String, failure_line_number: int = -1) -> GdUnitAssert:
	var line_number := failure_line_number if failure_line_number != -1 else GdUnitAssertions.get_line_number()
	_current_failure_message = GdAssertMessages.build_failure_message(error_message, _additional_failure_message, _custom_failure_message)
	GdAssertReports.report_error(_current_failure_message, line_number)
	return self


func _has_log_entry(log_entries: Array[ErrorLogEntry], type: ErrorLogEntry.TYPE, error: Variant) -> bool:
	for entry in log_entries:
		if entry._type == type and GdObjects.equals(entry._message, error):
			# Erase the log entry we already handled it by this assertion, otherwise it will report at twice
			_error_monitor().erase_log_entry(entry)
			return true
	return false


func _to_list(log_entries: Array[ErrorLogEntry]) -> String:
	if log_entries.is_empty():
		return "no errors"
	if log_entries.size() == 1:
		return log_entries[0]._message
	var value := ""
	for entry in log_entries:
		value += "'%s'\n" % entry._message
	return value


func is_null() -> GdUnitGodotErrorAssert:
	return _report_error("Not implemented")


func is_not_null() -> GdUnitGodotErrorAssert:
	return _report_error("Not implemented")


func is_equal(_expected: Variant) -> GdUnitGodotErrorAssert:
	return _report_error("Not implemented")


func is_not_equal(_expected: Variant) -> GdUnitGodotErrorAssert:
	return _report_error("Not implemented")


func override_failure_message(message: String) -> GdUnitGodotErrorAssert:
	_custom_failure_message = message
	return self


func append_failure_message(message: String) -> GdUnitGodotErrorAssert:
	_additional_failure_message = message
	return self


func is_success() -> GdUnitGodotErrorAssert:
	var log_entries := await _execute()
	if log_entries.is_empty():
		return _report_success()
	return _report_error("""
		Expecting: no error's are ocured.
			but found: '%s'
		""".dedent().trim_prefix("\n") % _to_list(log_entries))


func is_runtime_error(expected_error: Variant) -> GdUnitGodotErrorAssert:
	var result := GdUnitArgumentMatchers.is_variant_string_matching(expected_error)
	if result.is_error():
		return _report_error(result.error_message())
	var log_entries := await _execute()
	if _has_log_entry(log_entries, ErrorLogEntry.TYPE.SCRIPT_ERROR, expected_error):
		return _report_success()
	return _report_error("""
		Expecting: a runtime error is triggered.
			message: '%s'
			found: %s
		""".dedent().trim_prefix("\n") % [expected_error, _to_list(log_entries)])


func is_push_warning(expected_warning: Variant) -> GdUnitGodotErrorAssert:
	var result := GdUnitArgumentMatchers.is_variant_string_matching(expected_warning)
	if result.is_error():
		return _report_error(result.error_message())
	var log_entries := await _execute()
	if _has_log_entry(log_entries, ErrorLogEntry.TYPE.PUSH_WARNING, expected_warning):
		return _report_success()
	return _report_error("""
		Expecting: push_warning() is called.
			message: '%s'
			found: %s
		""".dedent().trim_prefix("\n") % [expected_warning, _to_list(log_entries)])


func is_push_error(expected_error: Variant) -> GdUnitGodotErrorAssert:
	var result := GdUnitArgumentMatchers.is_variant_string_matching(expected_error)
	if result.is_error():
		return _report_error(result.error_message())
	var log_entries := await _execute()
	if _has_log_entry(log_entries, ErrorLogEntry.TYPE.PUSH_ERROR, expected_error):
		return _report_success()
	return _report_error("""
		Expecting: push_error() is called.
			message: '%s'
			found: %s
		""".dedent().trim_prefix("\n") % [expected_error, _to_list(log_entries)])
