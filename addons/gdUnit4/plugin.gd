@tool
extends EditorPlugin

const GdUnitTools := preload ("res://addons/gdUnit4/src/core/GdUnitTools.gd")
const GdUnitTestDiscoverGuard := preload ("res://addons/gdUnit4/src/core/discovery/GdUnitTestDiscoverGuard.gd")


var _gd_inspector: Control
var _gd_console: Control
var _guard: GdUnitTestDiscoverGuard


func _enter_tree() -> void:
	if check_running_in_test_env():
		@warning_ignore("return_value_discarded")
		CmdConsole.new().prints_warning("It was recognized that GdUnit4 is running in a test environment, therefore the GdUnit4 plugin will not be executed!")
		return
	if Engine.get_version_info().hex < 0x40200:
		prints("GdUnit4 plugin requires a minimum of Godot 4.2.x Version!")
		return
	GdUnitSettings.setup()
	# install the GdUnit inspector
	_gd_inspector = load("res://addons/gdUnit4/src/ui/GdUnitInspector.tscn").instantiate()
	add_control_to_dock(EditorPlugin.DOCK_SLOT_LEFT_UR, _gd_inspector)
	# install the GdUnit Console
	_gd_console = load("res://addons/gdUnit4/src/ui/GdUnitConsole.tscn").instantiate()
	@warning_ignore("return_value_discarded")
	add_control_to_bottom_panel(_gd_console, "gdUnitConsole")
	prints("Loading GdUnit4 Plugin success")
	if GdUnitSettings.is_update_notification_enabled():
		var update_tool: Node = load("res://addons/gdUnit4/src/update/GdUnitUpdateNotify.tscn").instantiate()
		Engine.get_main_loop().root.add_child.call_deferred(update_tool)
	if GdUnit4CSharpApiLoader.is_mono_supported():
		prints("GdUnit4Net version '%s' loaded." % GdUnit4CSharpApiLoader.version())
	# connect to be notified for script changes to be able to discover new tests
	_guard = GdUnitTestDiscoverGuard.new()
	@warning_ignore("return_value_discarded")
	resource_saved.connect(_on_resource_saved)


func _exit_tree() -> void:
	if check_running_in_test_env():
		return
	if is_instance_valid(_gd_inspector):
		remove_control_from_docks(_gd_inspector)
		_gd_inspector.free()
	if is_instance_valid(_gd_console):
		remove_control_from_bottom_panel(_gd_console)
		_gd_console.free()
	GdUnitTools.dispose_all(true)
	prints("Unload GdUnit4 Plugin success")


func check_running_in_test_env() -> bool:
	var args := OS.get_cmdline_args()
	args.append_array(OS.get_cmdline_user_args())
	return DisplayServer.get_name() == "headless" or args.has("--selftest") or args.has("--add") or args.has("-a") or args.has("--quit-after") or args.has("--import")


func _on_resource_saved(resource: Resource) -> void:
	if resource is Script:
		await _guard.discover(resource as Script)
