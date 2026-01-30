class_name GodotGdErrorMonitor
extends GdUnitMonitor

var _report_enabled := false
var _logger: Logger


class GdUnitLogger extends Logger:
	var _entries: Array[ErrorLogEntry] = []
	var _line_number: int


	func entries() -> Array[ErrorLogEntry]:
		return _entries


	func _log_error(function: String, file: String, line: int, message: String, rationale: String, editor_notify: bool, error_type: int, script_backtraces: Array[ScriptBacktrace]) -> void:
		match error_type:
			ErrorType.ERROR_TYPE_WARNING:
				var stack_trace := _build_stack_trace(script_backtraces)
				_entries.append(ErrorLogEntry.of_push_warning(file, _line_number, message, stack_trace))

			ErrorType.ERROR_TYPE_ERROR:
				var stack_trace := _build_stack_trace(script_backtraces)
				_entries.append(ErrorLogEntry.of_push_error(file, _line_number, message, stack_trace))

			ErrorType.ERROR_TYPE_SCRIPT:
				var stack_trace := _build_stack_trace(script_backtraces)
				_entries.append(ErrorLogEntry.of_script_error(file, _line_number, message, stack_trace))

			ErrorType.ERROR_TYPE_SHADER:
				pass
			_:
				prints("Unknwon log type", message)

	func _log_message(message: String, error: bool) -> void:
		pass

	func _build_stack_trace(script_backtraces: Array[ScriptBacktrace]) -> PackedStringArray:
		for sb in script_backtraces:
			for frame in sb.get_frame_count():
				# Find start of test stack
				if sb.get_frame_file(frame) == "res://addons/gdUnit4/src/core/_TestCase.gd":
					var stack_trace := PackedStringArray()
					for test_case_frame in range(0, frame):
						_line_number = sb.get_frame_line(test_case_frame)
						stack_trace.append("	at %s:%s" % [sb.get_frame_file(test_case_frame), sb.get_frame_line(test_case_frame)])
					return stack_trace
		# if no stack trace collected, we in an await function call
		var sb := script_backtraces[0]
		return ["	at %s:%s" % [sb.get_frame_file(0), sb.get_frame_line(0)]]


func _init() -> void:
	super("GdUnitLoggerMonitor")
	_report_enabled = _is_reporting_enabled()
	_logger = GdUnitLogger.new()
	OS.add_logger(_logger)


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		if _logger:
			OS.remove_logger(_logger)


func start() -> void:
	clear_logs()


func stop() -> void:
	pass


func log_entries() -> Array[ErrorLogEntry]:
	return _logger.entries()


func to_reports() -> Array[GdUnitReport]:
	var reports_: Array[GdUnitReport] = []
	if _report_enabled:
		reports_.assign(log_entries().map(_to_report))

	return reports_


static func _to_report(errorLog: ErrorLogEntry) -> GdUnitReport:
	var failure := """
		%s
		%s %s
		%s""".dedent().trim_prefix("\n") % [
		GdAssertMessages._error("Godot Runtime Error !"),
		GdAssertMessages._error("Error:"),
		GdAssertMessages._colored_value(errorLog._message),
		GdAssertMessages._colored(errorLog._details, GdAssertMessages.VALUE_COLOR)]
	return GdUnitReport.new().create(GdUnitReport.ABORT, errorLog._line, failure)


func clear_logs() -> void:
	log_entries().clear()


func _is_reporting_enabled() -> bool:
	return _is_report_script_errors() or _is_report_push_errors()


func _is_report_push_errors() -> bool:
	return GdUnitSettings.is_report_push_errors()


func _is_report_script_errors() -> bool:
	return GdUnitSettings.is_report_script_errors()
