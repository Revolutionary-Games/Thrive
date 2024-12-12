@tool
extends PanelContainer

signal run_overall_pressed(debug: bool)
signal run_pressed(debug: bool)
signal stop_pressed()

@onready var _version_label: Control = %version
@onready var _button_wiki: Button = %help
@onready var _tool_button: Button = %tool
@onready var _button_run_overall: Button = %run_overall
@onready var _button_run: Button = %run
@onready var _button_run_debug: Button = %debug
@onready var _button_stop: Button = %stop



const SETTINGS_SHORTCUT_MAPPING := {
	GdUnitSettings.SHORTCUT_INSPECTOR_RERUN_TEST: GdUnitShortcut.ShortCut.RERUN_TESTS,
	GdUnitSettings.SHORTCUT_INSPECTOR_RERUN_TEST_DEBUG: GdUnitShortcut.ShortCut.RERUN_TESTS_DEBUG,
	GdUnitSettings.SHORTCUT_INSPECTOR_RUN_TEST_OVERALL: GdUnitShortcut.ShortCut.RUN_TESTS_OVERALL,
	GdUnitSettings.SHORTCUT_INSPECTOR_RUN_TEST_STOP: GdUnitShortcut.ShortCut.STOP_TEST_RUN,
}


@warning_ignore("return_value_discarded")
func _ready() -> void:
	GdUnit4Version.init_version_label(_version_label)
	var command_handler := GdUnitCommandHandler.instance()
	run_pressed.connect(command_handler._on_run_pressed)
	run_overall_pressed.connect(command_handler._on_run_overall_pressed)
	stop_pressed.connect(command_handler._on_stop_pressed)
	command_handler.gdunit_runner_start.connect(_on_gdunit_runner_start)
	command_handler.gdunit_runner_stop.connect(_on_gdunit_runner_stop)
	GdUnitSignals.instance().gdunit_settings_changed.connect(_on_gdunit_settings_changed)
	init_buttons()
	init_shortcuts(command_handler)



func init_buttons() -> void:
	_button_run_overall.icon = GdUnitUiTools.get_run_overall_icon()
	_button_run_overall.visible = GdUnitSettings.is_inspector_toolbar_button_show()
	_button_run.icon = GdUnitUiTools.get_icon("Play")
	_button_run_debug.icon = GdUnitUiTools.get_icon("PlayStart")
	_button_stop.icon = GdUnitUiTools.get_icon("Stop")
	_tool_button.icon = GdUnitUiTools.get_icon("Tools")
	_button_wiki.icon = GdUnitUiTools.get_icon("HelpSearch")


func init_shortcuts(command_handler: GdUnitCommandHandler) -> void:
	_button_run.shortcut = command_handler.get_shortcut(GdUnitShortcut.ShortCut.RERUN_TESTS)
	_button_run_overall.shortcut = command_handler.get_shortcut(GdUnitShortcut.ShortCut.RUN_TESTS_OVERALL)
	_button_run_debug.shortcut = command_handler.get_shortcut(GdUnitShortcut.ShortCut.RERUN_TESTS_DEBUG)
	_button_stop.shortcut = command_handler.get_shortcut(GdUnitShortcut.ShortCut.STOP_TEST_RUN)
	# register for shortcut changes
	@warning_ignore("return_value_discarded")
	GdUnitSignals.instance().gdunit_settings_changed.connect(_on_settings_changed.bind(command_handler))


func _on_runoverall_pressed(debug:=false) -> void:
	run_overall_pressed.emit(debug)


func _on_run_pressed(debug := false) -> void:
	run_pressed.emit(debug)


func _on_stop_pressed() -> void:
	stop_pressed.emit()


func _on_gdunit_runner_start() -> void:
	_button_run_overall.disabled = true
	_button_run.disabled = true
	_button_run_debug.disabled = true
	_button_stop.disabled = false


func _on_gdunit_runner_stop(_client_id: int) -> void:
	_button_run_overall.disabled = false
	_button_run.disabled = false
	_button_run_debug.disabled = false
	_button_stop.disabled = true


func _on_gdunit_settings_changed(_property: GdUnitProperty) -> void:
	_button_run_overall.visible = GdUnitSettings.is_inspector_toolbar_button_show()


func _on_wiki_pressed() -> void:
	@warning_ignore("return_value_discarded")
	OS.shell_open("https://mikeschulze.github.io/gdUnit4/")


func _on_btn_tool_pressed() -> void:
	var settings_dlg: Window = EditorInterface.get_base_control().find_child("GdUnitSettingsDialog", false, false)
	if settings_dlg == null:
		settings_dlg = preload("res://addons/gdUnit4/src/ui/settings/GdUnitSettingsDialog.tscn").instantiate()
		EditorInterface.get_base_control().add_child(settings_dlg, true)
	settings_dlg.popup_centered_ratio(.60)


func _on_settings_changed(property: GdUnitProperty, command_handler: GdUnitCommandHandler) -> void:
	# needs to wait a frame to be command handler notified first for settings changes
	await get_tree().process_frame
	if SETTINGS_SHORTCUT_MAPPING.has(property.name()):
		var shortcut: GdUnitShortcut.ShortCut = SETTINGS_SHORTCUT_MAPPING.get(property.name(), GdUnitShortcut.ShortCut.NONE)
		match shortcut:
			GdUnitShortcut.ShortCut.RERUN_TESTS:
				_button_run.shortcut = command_handler.get_shortcut(shortcut)
			GdUnitShortcut.ShortCut.RUN_TESTS_OVERALL:
				_button_run_overall.shortcut = command_handler.get_shortcut(shortcut)
			GdUnitShortcut.ShortCut.RERUN_TESTS_DEBUG:
				_button_run_debug.shortcut = command_handler.get_shortcut(shortcut)
			GdUnitShortcut.ShortCut.STOP_TEST_RUN:
				_button_stop.shortcut = command_handler.get_shortcut(shortcut)
