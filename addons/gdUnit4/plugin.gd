@tool
extends EditorPlugin

# We need to define manually the slot id's, to be downwards compatible
const CONTEXT_SLOT_FILESYSTEM: int = 1 # EditorContextMenuPlugin.CONTEXT_SLOT_FILESYSTEM
const CONTEXT_SLOT_SCRIPT_EDITOR: int = 2 # EditorContextMenuPlugin.CONTEXT_SLOT_SCRIPT_EDITOR

var _gd_inspector: Control
var _gd_console: Control
var _gd_filesystem_context_menu: Variant
var _gd_scripteditor_context_menu: Variant


func _enter_tree() -> void:

	var inferred_declaration: int = ProjectSettings.get_setting("debug/gdscript/warnings/inferred_declaration")
	var exclude_addons: bool = ProjectSettings.get_setting("debug/gdscript/warnings/exclude_addons")
	if !exclude_addons and inferred_declaration != 0:
		printerr("GdUnit4: 'inferred_declaration' is set to Warning/Error!")
		printerr("GdUnit4 is not 'inferred_declaration' save, you have to excluded addons (debug/gdscript/warnings/exclude_addons)")
		printerr("Loading GdUnit4 Plugin failed.")
		return

	if check_running_in_test_env():
		@warning_ignore("return_value_discarded")
		GdUnitCSIMessageWriter.new().prints_warning("It was recognized that GdUnit4 is running in a test environment, therefore the GdUnit4 plugin will not be executed!")
		return

	if Engine.get_version_info().hex < 0x40500:
		prints("This GdUnit4 plugin version '%s' requires Godot version '4.5' or higher to run." % GdUnit4Version.current())
		return
	GdUnitSettings.setup()
	# Install the GdUnit Inspector
	_gd_inspector = (load("res://addons/gdUnit4/src/ui/GdUnitInspector.tscn") as PackedScene).instantiate()
	_add_context_menus()
	add_control_to_dock(EditorPlugin.DOCK_SLOT_LEFT_UR, _gd_inspector)
	# Install the GdUnit Console
	_gd_console = (load("res://addons/gdUnit4/src/ui/GdUnitConsole.tscn") as PackedScene).instantiate()
	var control: Control = add_control_to_bottom_panel(_gd_console, "gdUnitConsole")
	@warning_ignore("unsafe_method_access")
	await _gd_console.setup_update_notification(control)
	if GdUnit4CSharpApiLoader.is_api_loaded():
		prints("GdUnit4Net version '%s' loaded." % GdUnit4CSharpApiLoader.version())
	else:
		prints("No GdUnit4Net found.")
	# Connect to be notified for script changes to be able to discover new tests
	GdUnitTestDiscoverGuard.instance()
	@warning_ignore("return_value_discarded")
	resource_saved.connect(_on_resource_saved)
	prints("Loading GdUnit4 Plugin success")


func _exit_tree() -> void:
	if check_running_in_test_env():
		return
	if is_instance_valid(_gd_inspector):
		remove_control_from_docks(_gd_inspector)
		_gd_inspector.free()
	_remove_context_menus()
	if is_instance_valid(_gd_console):
		remove_control_from_bottom_panel(_gd_console)
		_gd_console.free()
	var gdUnitTools: GDScript = load("res://addons/gdUnit4/src/core/GdUnitTools.gd")
	@warning_ignore("unsafe_method_access")
	gdUnitTools.dispose_all(true)
	prints("Unload GdUnit4 Plugin success")


func check_running_in_test_env() -> bool:
	var args: PackedStringArray = OS.get_cmdline_args()
	args.append_array(OS.get_cmdline_user_args())
	return DisplayServer.get_name() == "headless" or args.has("--selftest") or args.has("--add") or args.has("-a") or args.has("--quit-after") or args.has("--import")


func _add_context_menus() -> void:
	if Engine.get_version_info().hex >= 0x40400:
		# With Godot 4.4 we have to use the 'add_context_menu_plugin' to register editor context menus
		_gd_filesystem_context_menu = _preload_gdx_script("res://addons/gdUnit4/src/ui/menu/EditorFileSystemContextMenuHandlerV44.gdx")
		call_deferred("add_context_menu_plugin", CONTEXT_SLOT_FILESYSTEM, _gd_filesystem_context_menu)
		# the CONTEXT_SLOT_SCRIPT_EDITOR is adding to the script panel instead of script editor see https://github.com/godotengine/godot/pull/100556
		#_gd_scripteditor_context_menu = _preload("res://addons/gdUnit4/src/ui/menu/ScriptEditorContextMenuHandlerV44.gdx")
		#call_deferred("add_context_menu_plugin", CONTEXT_SLOT_SCRIPT_EDITOR, _gd_scripteditor_context_menu)
		# so we use the old hacky way to add the context menu
		_gd_inspector.add_child(preload("res://addons/gdUnit4/src/ui/menu/ScriptEditorContextMenuHandler.gd").new())
	else:
		# TODO Delete it if the minimum requirement for the plugin is set to Godot 4.4.
		_gd_inspector.add_child(preload("res://addons/gdUnit4/src/ui/menu/EditorFileSystemContextMenuHandler.gd").new())
		_gd_inspector.add_child(preload("res://addons/gdUnit4/src/ui/menu/ScriptEditorContextMenuHandler.gd").new())


func _remove_context_menus() -> void:
	if is_instance_valid(_gd_filesystem_context_menu):
		call_deferred("remove_context_menu_plugin", _gd_filesystem_context_menu)
	if is_instance_valid(_gd_scripteditor_context_menu):
		call_deferred("remove_context_menu_plugin", _gd_scripteditor_context_menu)


func _preload_gdx_script(script_path: String) -> Variant:
	var script: GDScript = GDScript.new()
	script.source_code = GdUnitFileAccess.resource_as_string(script_path)
	script.take_over_path(script_path)
	var err :Error = script.reload()
	if err != OK:
		push_error("Can't create context menu %s, error: %s" % [script_path, error_string(err)])
	return script.new()


func _on_resource_saved(resource: Resource) -> void:
	if resource is Script:
		await GdUnitTestDiscoverGuard.instance().discover(resource as Script)
