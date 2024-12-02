@tool
class_name GdUnitInspecor
extends Panel

const ScriptEditorContextMenuHandler = preload("res://addons/gdUnit4/src/ui/menu/ScriptEditorContextMenuHandler.gd")
const EditorFileSystemContextMenuHandler = preload("res://addons/gdUnit4/src/ui/menu/EditorFileSystemContextMenuHandler.gd")

var _command_handler := GdUnitCommandHandler.instance()


func _ready() -> void:
	if Engine.is_editor_hint():
		_getEditorThemes()
	@warning_ignore("return_value_discarded")
	GdUnitCommandHandler.instance().gdunit_runner_start.connect(func() -> void:
		var control :Control = get_parent_control()
		# if the tab is floating we dont need to set as current
		if control is TabContainer:
			var tab_container :TabContainer = control
			for tab_index in tab_container.get_tab_count():
				if tab_container.get_tab_title(tab_index) == "GdUnit":
					tab_container.set_current_tab(tab_index)
	)
	if Engine.is_editor_hint():
		add_script_editor_context_menu()
		add_file_system_dock_context_menu()


func _process(_delta: float) -> void:
	_command_handler._do_process()


func _getEditorThemes() -> void:
	# example to access current theme
	#var editiorTheme := interface.get_base_control().theme
	# setup inspector button icons
	#var stylebox_types :PackedStringArray = editiorTheme.get_stylebox_type_list()
	#for stylebox_type in stylebox_types:
		#prints("stylebox_type", stylebox_type)
	#	if "Tree" == stylebox_type:
	#		prints(editiorTheme.get_stylebox_list(stylebox_type))
	#var style:StyleBoxFlat = editiorTheme.get_stylebox("panel", "Tree")
	#style.bg_color = Color.RED
	#var locale = interface.get_editor_settings().get_setting("interface/editor/editor_language")
	#sessions_label.add_theme_color_override("font_color", get_color("contrast_color_2", "Editor"))
	#status_label.add_theme_color_override("font_color", get_color("contrast_color_2", "Editor"))
	#no_sessions_label.add_theme_color_override("font_color", get_color("contrast_color_2", "Editor"))
	pass


# Context menu registrations ----------------------------------------------------------------------
func add_file_system_dock_context_menu() -> void:
	var is_test_suite := func is_visible(script: Script, is_ts: bool) -> bool:
		if script == null:
			return false
		return GdObjects.is_test_suite(script) == is_ts
	var menu :Array[GdUnitContextMenuItem] = [
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.TEST_RUN, "Run Testsuites", "Play", is_test_suite.bind(true), _command_handler.command(GdUnitCommandHandler.CMD_RUN_TESTSUITE)),
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.TEST_DEBUG, "Debug Testsuites", "PlayStart", is_test_suite.bind(true), _command_handler.command(GdUnitCommandHandler.CMD_RUN_TESTSUITE_DEBUG)),
	]
	add_child(EditorFileSystemContextMenuHandler.new(menu))


func add_script_editor_context_menu() -> void:
	var is_test_suite := func is_visible(script: Script, is_ts: bool) -> bool:
		return GdObjects.is_test_suite(script) == is_ts
	var menu :Array[GdUnitContextMenuItem] = [
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.TEST_RUN, "Run Tests", "Play", is_test_suite.bind(true), _command_handler.command(GdUnitCommandHandler.CMD_RUN_TESTCASE)),
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.TEST_DEBUG, "Debug Tests", "PlayStart", is_test_suite.bind(true),_command_handler.command(GdUnitCommandHandler.CMD_RUN_TESTCASE_DEBUG)),
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.CREATE_TEST, "Create Test", "New", is_test_suite.bind(false), _command_handler.command(GdUnitCommandHandler.CMD_CREATE_TESTCASE))
	]
	add_child(ScriptEditorContextMenuHandler.new(menu))


func _on_MainPanel_run_testsuite(test_suite_paths: Array, debug: bool) -> void:
	_command_handler.cmd_run_test_suites(test_suite_paths, debug)


func _on_MainPanel_run_testcase(resource_path: String, test_case: String, test_param_index: int, debug: bool) -> void:
	_command_handler.cmd_run_test_case(resource_path, test_case, test_param_index, debug)


@warning_ignore("redundant_await")
func _on_status_bar_request_discover_tests() -> void:
	await _command_handler.cmd_discover_tests()
