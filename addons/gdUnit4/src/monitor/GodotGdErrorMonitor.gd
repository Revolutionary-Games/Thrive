class_name GodotGdErrorMonitor
extends GdUnitMonitor

var _godot_log_file: String
var _eof: int
var _report_enabled := false
var _entries: Array[ErrorLogEntry] = []


func _init() -> void:
	super("GodotGdErrorMonitor")
	_godot_log_file = GdUnitSettings.get_log_path()
	_report_enabled = _is_reporting_enabled()


func start() -> void:
	var file := FileAccess.open(_godot_log_file, FileAccess.READ)
	if file:
		file.seek_end(0)
		_eof = file.get_length()


func stop() -> void:
	pass


func to_reports() -> Array[GdUnitReport]:
	var reports_: Array[GdUnitReport] = []
	if _report_enabled:
		reports_.assign(_entries.map(_to_report))
	_entries.clear()
	return reports_


static func _to_report(errorLog: ErrorLogEntry) -> GdUnitReport:
	var failure := "%s\n\t%s\n%s %s" % [
		GdAssertMessages._error("Godot Runtime Error !"),
		GdAssertMessages._colored_value(errorLog._details),
		GdAssertMessages._error("Error:"),
		GdAssertMessages._colored_value(errorLog._message)]
	return GdUnitReport.new().create(GdUnitReport.ABORT, errorLog._line, failure)


func scan(force_collect_reports := false) -> Array[ErrorLogEntry]:
	await (Engine.get_main_loop() as SceneTree).process_frame
	await (Engine.get_main_loop() as SceneTree).physics_frame
	_entries.append_array(_collect_log_entries(force_collect_reports))
	return _entries


func erase_log_entry(entry: ErrorLogEntry) -> void:
	_entries.erase(entry)


func collect_full_logs() -> PackedStringArray:
	await (Engine.get_main_loop() as SceneTree).process_frame
	await (Engine.get_main_loop() as SceneTree).physics_frame

	var file := FileAccess.open(_godot_log_file, FileAccess.READ)
	file.seek(_eof)
	var records := PackedStringArray()
	while not file.eof_reached():
		@warning_ignore("return_value_discarded")
		records.append(file.get_line())

	return records


func _collect_log_entries(force_collect_reports: bool) -> Array[ErrorLogEntry]:
	var file := FileAccess.open(_godot_log_file, FileAccess.READ)
	file.seek(_eof)
	var records := PackedStringArray()
	while not file.eof_reached():
		@warning_ignore("return_value_discarded")
		records.append(file.get_line())
	file.seek_end(0)
	_eof = file.get_length()
	var log_entries: Array[ErrorLogEntry]= []
	var is_report_errors := force_collect_reports or _is_report_push_errors()
	var is_report_script_errors := force_collect_reports or _is_report_script_errors()
	for index in records.size():
		if force_collect_reports:
			log_entries.append(ErrorLogEntry.extract_push_warning(records, index))
		if is_report_errors:
			log_entries.append(ErrorLogEntry.extract_push_error(records, index))
		if is_report_script_errors:
			log_entries.append(ErrorLogEntry.extract_error(records, index))
	return log_entries.filter(func(value: ErrorLogEntry) -> bool: return value != null )


func _is_reporting_enabled() -> bool:
	return _is_report_script_errors() or _is_report_push_errors()


func _is_report_push_errors() -> bool:
	return GdUnitSettings.is_report_push_errors()


func _is_report_script_errors() -> bool:
	return GdUnitSettings.is_report_script_errors()
