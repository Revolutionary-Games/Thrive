@tool
extends Control

const GdUnitUpdateClient = preload("res://addons/gdUnit4/src/update/GdUnitUpdateClient.gd")
const TITLE = "gdUnit4 ${version} Console"

@onready var header := $VBoxContainer/Header
@onready var title: RichTextLabel = $VBoxContainer/Header/header_title
@onready var output: RichTextLabel = $VBoxContainer/Console/TextEdit

var _text_color: Color
var _function_color: Color
var _engine_type_color: Color
var _statistics := {}
var _summary := {
	"total_count": 0,
	"error_count": 0,
	"failed_count": 0,
	"skipped_count": 0,
	"flaky_count": 0,
	"orphan_nodes": 0
}


@warning_ignore("return_value_discarded")
func _ready() -> void:
	init_colors()
	GdUnitFonts.init_fonts(output)
	GdUnit4Version.init_version_label(title)
	GdUnitSignals.instance().gdunit_event.connect(_on_gdunit_event)
	GdUnitSignals.instance().gdunit_message.connect(_on_gdunit_message)
	GdUnitSignals.instance().gdunit_client_connected.connect(_on_gdunit_client_connected)
	GdUnitSignals.instance().gdunit_client_disconnected.connect(_on_gdunit_client_disconnected)
	output.clear()


func _notification(what: int) -> void:
	if what == EditorSettings.NOTIFICATION_EDITOR_SETTINGS_CHANGED:
		init_colors()
	if what == NOTIFICATION_PREDELETE:
		var instance := GdUnitSignals.instance()
		if instance.gdunit_event.is_connected(_on_gdunit_event):
			instance.gdunit_event.disconnect(_on_gdunit_event)
		if instance.gdunit_message.is_connected(_on_gdunit_event):
			instance.gdunit_message.disconnect(_on_gdunit_message)
		if instance.gdunit_client_connected.is_connected(_on_gdunit_event):
			instance.gdunit_client_connected.disconnect(_on_gdunit_client_connected)
		if instance.gdunit_client_disconnected.is_connected(_on_gdunit_event):
			instance.gdunit_client_disconnected.disconnect(_on_gdunit_client_disconnected)


func init_colors() -> void:
	var settings := EditorInterface.get_editor_settings()
	_text_color = settings.get_setting("text_editor/theme/highlighting/text_color")
	_function_color = settings.get_setting("text_editor/theme/highlighting/function_color")
	_engine_type_color = settings.get_setting("text_editor/theme/highlighting/engine_type_color")


func init_statistics(event: GdUnitEvent) -> void:
	_statistics["total_count"] = event.total_count()
	_statistics["error_count"] = 0
	_statistics["failed_count"] = 0
	_statistics["skipped_count"] = 0
	_statistics["flaky_count"] = 0
	_statistics["orphan_nodes"] = 0
	_summary["total_count"] += event.total_count()


func reset_statistics() -> void:
	for k: String in _statistics.keys():
		_statistics[k] = 0
	for k: String in _summary.keys():
		_summary[k] = 0


func update_statistics(event: GdUnitEvent) -> void:
	_statistics["error_count"] += event.error_count()
	_statistics["failed_count"] += event.failed_count()
	_statistics["skipped_count"] += event.is_skipped() as int
	_statistics["flaky_count"] += event.is_flaky() as int
	_statistics["orphan_nodes"] += event.orphan_nodes()
	_summary["error_count"] += event.error_count()
	_summary["failed_count"] += event.failed_count()
	_summary["skipped_count"] += event.is_skipped() as int
	_summary["flaky_count"] += event.is_flaky() as int
	_summary["orphan_nodes"] += event.orphan_nodes()


func print_message(message: String, color: Color=_text_color, indent:=0) -> void:
	for i in indent:
		output.push_indent(1)
	output.push_color(color)
	output.append_text(message)
	output.pop()
	for i in indent:
		output.pop()


func println_message(message: String, color: Color=_text_color, indent:=-1) -> void:
	print_message(message, color, indent)
	output.newline()


func line_number(report: GdUnitReport) -> String:
	return str(report._line_number) if report._line_number != -1 else "<n/a>"


func setup_update_notification(control: Button) -> void:
	if not GdUnitSettings.is_update_notification_enabled():
		print_message("The search for updates is deactivated.", Color.CORNFLOWER_BLUE)
		return

	println_message("Searching for updates.", Color.CORNFLOWER_BLUE)
	var update_client := GdUnitUpdateClient.new()
	add_child(update_client)
	var response :GdUnitUpdateClient.HttpResponse = await update_client.request_latest_version()
	if response.status() != 200:
		println_message("Information cannot be retrieved from GitHub!", Color.INDIAN_RED)
		println_message("Error:  %s" % response.response(), Color.INDIAN_RED)
		return
	var latest_version := update_client.extract_latest_version(response)
	if not latest_version.is_greater(GdUnit4Version.current()):
		println_message("GdUnit4 is up-to-date.", Color.FOREST_GREEN)
		return

	println_message("A new update is available %s" % latest_version, Color.YELLOW)
	println_message("Open the GdUnit4 settings and check the update tab.", Color.YELLOW)

	control.icon = GdUnitUiTools.get_icon("Notification", Color.YELLOW)
	var tween :=create_tween()
	tween.tween_property(control, "self_modulate", Color.VIOLET, .2).set_trans(Tween.TransitionType.TRANS_LINEAR)
	tween.tween_property(control, "self_modulate", Color.YELLOW, .2).set_trans(Tween.TransitionType.TRANS_BOUNCE)
	tween.parallel()
	tween.tween_property(control, "scale", Vector2.ONE*1.05, .4).set_trans(Tween.TransitionType.TRANS_LINEAR)
	tween.tween_property(control, "scale", Vector2.ONE, .4).set_trans(Tween.TransitionType.TRANS_BOUNCE)
	tween.set_loops(-1)
	tween.play()


func _on_gdunit_event(event: GdUnitEvent) -> void:
	match event.type():
		GdUnitEvent.INIT:
			reset_statistics()

		GdUnitEvent.STOP:
			print_message("Summary:", Color.DODGER_BLUE)
			println_message("| %d total | %d error | %d failed | %d flaky | %d skipped | %d orphans |" %\
				[_summary["total_count"],
				_summary["error_count"],
				_summary["failed_count"],
				_summary["flaky_count"],
				_summary["skipped_count"],
				_summary["orphan_nodes"]],
				_text_color, 1)
			print_message("[wave][/wave]")

		GdUnitEvent.TESTSUITE_BEFORE:
			init_statistics(event)
			print_message("Execute: ", Color.DODGER_BLUE)
			println_message(event._suite_name, _engine_type_color)

		GdUnitEvent.TESTSUITE_AFTER:
			if not event.reports().is_empty():
				println_message("\t" + event._suite_name, _engine_type_color)
				for report: GdUnitReport in event.reports():
					println_message("line %s: %s" % [line_number(report), report._message], _text_color, 2)
			if event.is_success() and event.is_flaky():
				print_message("[wave]FLAKY[/wave]", Color.GREEN_YELLOW)
			elif event.is_success():
				print_message("[wave]PASSED[/wave]", Color.LIGHT_GREEN)
			else:
				print_message("[shake rate=5 level=10][b]FAILED[/b][/shake]", Color.FIREBRICK)
			print_message(" | %d total | %d error | %d failed | %d flaky | %d skipped | %d orphans |" %\
				[_statistics["total_count"],
				_statistics["error_count"],
				_statistics["failed_count"],
				_statistics["flaky_count"],
				_statistics["skipped_count"],
				_statistics["orphan_nodes"]])
			println_message("%+12s" % LocalTime.elapsed(event.elapsed_time()))
			println_message(" ")


		GdUnitEvent.TESTCASE_BEFORE:
			var spaces := "-%d" % (80 - event._suite_name.length())
			print_message(event._suite_name, _engine_type_color, 1)
			print_message(":")
			print_message(("%" + spaces + "s") % event._test_name, _function_color)

		GdUnitEvent.TESTCASE_AFTER:
			var reports := event.reports()
			if event.is_flaky() and event.is_success():
				var retries :int = event.statistic(GdUnitEvent.RETRY_COUNT)
				print_message("[wave]FLAKY[/wave] (%d retries)" % retries, Color.GREEN_YELLOW)
			elif event.is_success():
				print_message("PASSED", Color.LIGHT_GREEN)
			elif event.is_skipped():
				print_message("SKIPPED", Color.GOLDENROD)
			elif event.is_error() or event.is_failed():
				var retries :int = event.statistic(GdUnitEvent.RETRY_COUNT)
				if retries > 1:
					print_message("[wave]FAILED[/wave] (retry %d)" % retries, Color.FIREBRICK)
				else:
					print_message("[wave]FAILED[/wave]", Color.FIREBRICK)
			elif event.is_warning():
				print_message("WARNING", Color.YELLOW)
			println_message(" %+12s" % LocalTime.elapsed(event.elapsed_time()))

			for report: GdUnitReport in event.reports():
				println_message("line %s: %s" % [line_number(report), report._message], _text_color, 2)

		GdUnitEvent.TESTCASE_STATISTICS:
			update_statistics(event)


func _on_gdunit_client_connected(client_id: int) -> void:
	output.clear()
	output.append_text("[color=#9887c4]GdUnit Test Client connected with id %d[/color]\n" % client_id)
	output.newline()


func _on_gdunit_client_disconnected(client_id: int) -> void:
	output.append_text("[color=#9887c4]GdUnit Test Client disconnected with id %d[/color]\n" % client_id)
	output.newline()


func _on_gdunit_message(message: String) -> void:
	output.newline()
	output.append_text(message)
	output.newline()
