class_name GdUnitCommandHandler
extends Object

signal gdunit_runner_start()
signal gdunit_runner_stop(client_id :int)


const GdUnitTools := preload("res://addons/gdUnit4/src/core/GdUnitTools.gd")

const CMD_RUN_OVERALL = "Debug Overall TestSuites"
const CMD_RUN_TESTCASE = "Run TestCases"
const CMD_RUN_TESTCASE_DEBUG = "Run TestCases (Debug)"
const CMD_RUN_TESTSUITE = "Run TestSuites"
const CMD_RUN_TESTSUITE_DEBUG = "Run TestSuites (Debug)"
const CMD_RERUN_TESTS = "ReRun Tests"
const CMD_RERUN_TESTS_DEBUG = "ReRun Tests (Debug)"
const CMD_STOP_TEST_RUN = "Stop Test Run"
const CMD_CREATE_TESTCASE = "Create TestCase"

const SETTINGS_SHORTCUT_MAPPING := {
	"N/A" : GdUnitShortcut.ShortCut.NONE,
	GdUnitSettings.SHORTCUT_INSPECTOR_RERUN_TEST : GdUnitShortcut.ShortCut.RERUN_TESTS,
	GdUnitSettings.SHORTCUT_INSPECTOR_RERUN_TEST_DEBUG : GdUnitShortcut.ShortCut.RERUN_TESTS_DEBUG,
	GdUnitSettings.SHORTCUT_INSPECTOR_RUN_TEST_OVERALL : GdUnitShortcut.ShortCut.RUN_TESTS_OVERALL,
	GdUnitSettings.SHORTCUT_INSPECTOR_RUN_TEST_STOP : GdUnitShortcut.ShortCut.STOP_TEST_RUN,
	GdUnitSettings.SHORTCUT_EDITOR_RUN_TEST : GdUnitShortcut.ShortCut.RUN_TESTCASE,
	GdUnitSettings.SHORTCUT_EDITOR_RUN_TEST_DEBUG : GdUnitShortcut.ShortCut.RUN_TESTCASE_DEBUG,
	GdUnitSettings.SHORTCUT_EDITOR_CREATE_TEST : GdUnitShortcut.ShortCut.CREATE_TEST,
	GdUnitSettings.SHORTCUT_FILESYSTEM_RUN_TEST : GdUnitShortcut.ShortCut.RUN_TESTSUITE,
	GdUnitSettings.SHORTCUT_FILESYSTEM_RUN_TEST_DEBUG : GdUnitShortcut.ShortCut.RUN_TESTSUITE_DEBUG
}

# the current test runner config
var _runner_config := GdUnitRunnerConfig.new()

# holds the current connected gdUnit runner client id
var _client_id: int
# if no debug mode we have an process id
var _current_runner_process_id: int = 0
# hold is current an test running
var _is_running: bool = false
# holds if the current running tests started in debug mode
var _running_debug_mode: bool

var _commands := {}
var _shortcuts := {}


static func instance() -> GdUnitCommandHandler:
	return GdUnitSingleton.instance("GdUnitCommandHandler", func() -> GdUnitCommandHandler: return GdUnitCommandHandler.new())


@warning_ignore("return_value_discarded")
func _init() -> void:
	assert_shortcut_mappings(SETTINGS_SHORTCUT_MAPPING)

	GdUnitSignals.instance().gdunit_event.connect(_on_event)
	GdUnitSignals.instance().gdunit_client_connected.connect(_on_client_connected)
	GdUnitSignals.instance().gdunit_client_disconnected.connect(_on_client_disconnected)
	GdUnitSignals.instance().gdunit_settings_changed.connect(_on_settings_changed)
	# preload previous test execution
	@warning_ignore("return_value_discarded")
	_runner_config.load_config()

	init_shortcuts()
	var is_running := func(_script :Script) -> bool: return _is_running
	var is_not_running := func(_script :Script) -> bool: return !_is_running
	register_command(GdUnitCommand.new(CMD_RUN_OVERALL, is_not_running, cmd_run_overall.bind(true), GdUnitShortcut.ShortCut.RUN_TESTS_OVERALL))
	register_command(GdUnitCommand.new(CMD_RUN_TESTCASE, is_not_running, cmd_editor_run_test.bind(false), GdUnitShortcut.ShortCut.RUN_TESTCASE))
	register_command(GdUnitCommand.new(CMD_RUN_TESTCASE_DEBUG, is_not_running, cmd_editor_run_test.bind(true), GdUnitShortcut.ShortCut.RUN_TESTCASE_DEBUG))
	register_command(GdUnitCommand.new(CMD_RUN_TESTSUITE, is_not_running, cmd_run_test_suites.bind(false), GdUnitShortcut.ShortCut.RUN_TESTSUITE))
	register_command(GdUnitCommand.new(CMD_RUN_TESTSUITE_DEBUG, is_not_running, cmd_run_test_suites.bind(true), GdUnitShortcut.ShortCut.RUN_TESTSUITE_DEBUG))
	register_command(GdUnitCommand.new(CMD_RERUN_TESTS, is_not_running, cmd_run.bind(false), GdUnitShortcut.ShortCut.RERUN_TESTS))
	register_command(GdUnitCommand.new(CMD_RERUN_TESTS_DEBUG, is_not_running, cmd_run.bind(true), GdUnitShortcut.ShortCut.RERUN_TESTS_DEBUG))
	register_command(GdUnitCommand.new(CMD_CREATE_TESTCASE, is_not_running, cmd_create_test, GdUnitShortcut.ShortCut.CREATE_TEST))
	register_command(GdUnitCommand.new(CMD_STOP_TEST_RUN, is_running, cmd_stop.bind(_client_id), GdUnitShortcut.ShortCut.STOP_TEST_RUN))

	# schedule discover tests if enabled and running inside the editor
	if Engine.is_editor_hint() and GdUnitSettings.is_test_discover_enabled():
		var timer :SceneTreeTimer = (Engine.get_main_loop() as SceneTree).create_timer(5)
		@warning_ignore("return_value_discarded")
		timer.timeout.connect(cmd_discover_tests)


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		_commands.clear()
		_shortcuts.clear()


func _do_process() -> void:
	check_test_run_stopped_manually()


# is checking if the user has press the editor stop scene
func check_test_run_stopped_manually() -> void:
	if is_test_running_but_stop_pressed():
		if GdUnitSettings.is_verbose_assert_warnings():
			push_warning("Test Runner scene was stopped manually, force stopping the current test run!")
		cmd_stop(_client_id)


func is_test_running_but_stop_pressed() -> bool:
	return _running_debug_mode and _is_running and not EditorInterface.is_playing_scene()


func assert_shortcut_mappings(mappings: Dictionary) -> void:
	for shortcut: int in GdUnitShortcut.ShortCut.values():
		assert(mappings.values().has(shortcut), "missing settings mapping for shortcut '%s'!" % GdUnitShortcut.ShortCut.keys()[shortcut])


func init_shortcuts() -> void:
	for shortcut: int in GdUnitShortcut.ShortCut.values():
		if shortcut == GdUnitShortcut.ShortCut.NONE:
			continue
		var property_name: String = SETTINGS_SHORTCUT_MAPPING.find_key(shortcut)
		var property := GdUnitSettings.get_property(property_name)
		var keys := GdUnitShortcut.default_keys(shortcut)
		if property != null:
			keys = property.value()
		var inputEvent := create_shortcut_input_even(keys)
		register_shortcut(shortcut, inputEvent)


func create_shortcut_input_even(key_codes: PackedInt32Array) -> InputEventKey:
	var inputEvent := InputEventKey.new()
	inputEvent.pressed = true
	for key_code in key_codes:
		match key_code:
			KEY_ALT:
				inputEvent.alt_pressed = true
			KEY_SHIFT:
				inputEvent.shift_pressed = true
			KEY_CTRL:
				inputEvent.ctrl_pressed = true
			_:
				inputEvent.keycode = key_code as Key
				inputEvent.physical_keycode = key_code as Key
	return inputEvent


func register_shortcut(p_shortcut: GdUnitShortcut.ShortCut, p_input_event: InputEvent) -> void:
	GdUnitTools.prints_verbose("register shortcut: '%s' to '%s'" % [GdUnitShortcut.ShortCut.keys()[p_shortcut], p_input_event.as_text()])
	var shortcut := Shortcut.new()
	shortcut.set_events([p_input_event])
	var command_name := get_shortcut_command(p_shortcut)
	_shortcuts[p_shortcut] = GdUnitShortcutAction.new(p_shortcut, shortcut, command_name)


func get_shortcut(shortcut_type: GdUnitShortcut.ShortCut) -> Shortcut:
	return get_shortcut_action(shortcut_type).shortcut


func get_shortcut_action(shortcut_type: GdUnitShortcut.ShortCut) -> GdUnitShortcutAction:
	return _shortcuts.get(shortcut_type)


func get_shortcut_command(p_shortcut: GdUnitShortcut.ShortCut) -> String:
	return GdUnitShortcut.CommandMapping.get(p_shortcut, "unknown command")


func register_command(p_command: GdUnitCommand) -> void:
	_commands[p_command.name] = p_command


func command(cmd_name: String) -> GdUnitCommand:
	return _commands.get(cmd_name)


func cmd_run_test_suites(scripts: Array[Script], debug: bool, rerun := false) -> void:
	# Update test discovery
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverStart.new())
	var tests_to_execute: Array[GdUnitTestCase] = []
	for script in scripts:
		GdUnitTestDiscoverer.discover_tests(script, func(test_case: GdUnitTestCase) -> void:
			tests_to_execute.append(test_case)
			GdUnitTestDiscoverSink.discover(test_case)
		)
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverEnd.new(0, 0))
	GdUnitTestDiscoverer.console_log_discover_results(tests_to_execute)

	# create new runner runner_config for fresh run otherwise use saved one
	if not rerun:
		var result := _runner_config.clear()\
			.add_test_cases(tests_to_execute)\
			.save_config()
		if result.is_error():
			push_error(result.error_message())
			return
	cmd_run(debug)


func cmd_run_test_case(script: Script, test_case: String, test_param_index: int, debug: bool, rerun := false) -> void:
	# Update test discovery
	var tests_to_execute: Array[GdUnitTestCase] = []
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverStart.new())
	GdUnitTestDiscoverer.discover_tests(script, func(test: GdUnitTestCase) -> void:
		# We filter for a single test
		if test.test_name == test_case:
			# We only add selected parameterized test to the execution list
			if test_param_index == -1:
				tests_to_execute.append(test)
			elif test.attribute_index == test_param_index:
				tests_to_execute.append(test)
			GdUnitTestDiscoverSink.discover(test)
	)
	GdUnitSignals.instance().gdunit_event.emit(GdUnitEventTestDiscoverEnd.new(0, 0))
	GdUnitTestDiscoverer.console_log_discover_results(tests_to_execute)

	# create new runner config for fresh run otherwise use saved one
	if not rerun:
		var result := _runner_config.clear()\
			.add_test_cases(tests_to_execute)\
			.save_config()
		if result.is_error():
			push_error(result.error_message())
			return
	cmd_run(debug)


func cmd_run_tests(tests_to_execute: Array[GdUnitTestCase], debug: bool) -> void:
	# Save tests to runner config before execute
	var result := _runner_config.clear()\
		.add_test_cases(tests_to_execute)\
		.save_config()
	if result.is_error():
		push_error(result.error_message())
		return
	cmd_run(debug)


func cmd_run_overall(debug: bool) -> void:
	var tests_to_execute := await GdUnitTestDiscoverer.run()
	var result := _runner_config.clear()\
		.add_test_cases(tests_to_execute)\
		.save_config()
	if result.is_error():
		push_error(result.error_message())
		return
	cmd_run(debug)


func cmd_run(debug: bool) -> void:
	# don't start is already running
	if _is_running:
		return

	GdUnitSignals.instance().gdunit_event.emit(GdUnitInit.new())
	# save current selected excution config
	var server_port: int = Engine.get_meta("gdunit_server_port")
	var result := _runner_config.set_server_port(server_port).save_config()
	if result.is_error():
		push_error(result.error_message())
		return
	# before start we have to save all changes
	ScriptEditorControls.save_all_open_script()
	gdunit_runner_start.emit()
	_current_runner_process_id = -1
	_running_debug_mode = debug
	if debug:
		run_debug_mode()
	else:
		run_release_mode()


func cmd_stop(client_id: int) -> void:
	# don't stop if is already stopped
	if not _is_running:
		return
	_is_running = false
	gdunit_runner_stop.emit(client_id)
	if _running_debug_mode:
		EditorInterface.stop_playing_scene()
	elif _current_runner_process_id > 0:
		if OS.is_process_running(_current_runner_process_id):
			var result := OS.kill(_current_runner_process_id)
			if result != OK:
				push_error("ERROR checked stopping GdUnit Test Runner. error code: %s" % result)
	_current_runner_process_id = -1


func cmd_editor_run_test(debug: bool) -> void:
	if is_active_script_editor():
		var cursor_line := active_base_editor().get_caret_line()
		#run test case?
		var regex := RegEx.new()
		@warning_ignore("return_value_discarded")
		regex.compile("(^func[ ,\t])(test_[a-zA-Z0-9_]*)")
		var result := regex.search(active_base_editor().get_line(cursor_line))
		if result:
			var func_name := result.get_string(2).strip_edges()
			if func_name.begins_with("test_"):
				cmd_run_test_case(active_script(), func_name, -1, debug)
				return
	# otherwise run the full test suite
	var selected_test_suites: Array[Script] = [active_script()]
	cmd_run_test_suites(selected_test_suites, debug)


func cmd_create_test() -> void:
	if not is_active_script_editor():
		return
	var cursor_line := active_base_editor().get_caret_line()
	var result := GdUnitTestSuiteBuilder.create(active_script(), cursor_line)
	if result.is_error():
		# show error dialog
		push_error("Failed to create test case: %s" % result.error_message())
		return
	var info: Dictionary = result.value()
	var script_path: String = info.get("path")
	var script_line: int = info.get("line")
	ScriptEditorControls.edit_script(script_path, script_line)


func cmd_discover_tests() -> void:
	await GdUnitTestDiscoverer.run()


static func scan_all_test_directories(root: String) -> PackedStringArray:
	var base_directory := "res://"
	# If the test root folder is configured as blank, "/", or "res://", use the root folder as described in the settings panel
	if root.is_empty() or root == "/" or root == base_directory:
		return [base_directory]
	return scan_test_directories(base_directory, root, [])


static func scan_test_directories(base_directory: String, test_directory: String, test_suite_paths: PackedStringArray) -> PackedStringArray:
	print_verbose("Scannning for test directory '%s' at %s" % [test_directory, base_directory])
	for directory in DirAccess.get_directories_at(base_directory):
		if directory.begins_with("."):
			continue
		var current_directory := normalize_path(base_directory + "/" + directory)
		if GdUnitTestSuiteScanner.exclude_scan_directories.has(current_directory):
			continue
		if match_test_directory(directory, test_directory):
			@warning_ignore("return_value_discarded")
			test_suite_paths.append(current_directory)
		else:
			@warning_ignore("return_value_discarded")
			scan_test_directories(current_directory, test_directory, test_suite_paths)
	return test_suite_paths


static func normalize_path(path: String) -> String:
	return path.replace("///", "//")


static func match_test_directory(directory: String, test_directory: String) -> bool:
	return directory == test_directory or test_directory.is_empty() or test_directory == "/" or test_directory == "res://"


func run_debug_mode() -> void:
	EditorInterface.play_custom_scene("res://addons/gdUnit4/src/core/runners/GdUnitTestRunner.tscn")
	_is_running = true


func run_release_mode() -> void:
	var arguments := Array()
	if OS.is_stdout_verbose():
		arguments.append("--verbose")
	arguments.append("--no-window")
	arguments.append("--path")
	arguments.append(ProjectSettings.globalize_path("res://"))
	arguments.append("res://addons/gdUnit4/src/core/runners/GdUnitTestRunner.tscn")
	_current_runner_process_id = OS.create_process(OS.get_executable_path(), arguments, false);
	_is_running = true


func is_active_script_editor() -> bool:
	return EditorInterface.get_script_editor().get_current_editor() != null


func active_base_editor() -> TextEdit:
	return EditorInterface.get_script_editor().get_current_editor().get_base_editor()


func active_script() -> Script:
	return EditorInterface.get_script_editor().get_current_script()



################################################################################
# signals handles
################################################################################
func _on_event(event: GdUnitEvent) -> void:
	if event.type() == GdUnitEvent.STOP:
		cmd_stop(_client_id)


func _on_stop_pressed() -> void:
	cmd_stop(_client_id)


func _on_run_pressed(debug := false) -> void:
	cmd_run(debug)


func _on_run_overall_pressed(_debug := false) -> void:
	cmd_run_overall(true)


func _on_settings_changed(property: GdUnitProperty) -> void:
	if SETTINGS_SHORTCUT_MAPPING.has(property.name()):
		var shortcut :GdUnitShortcut.ShortCut = SETTINGS_SHORTCUT_MAPPING.get(property.name())
		var value: PackedInt32Array = property.value()
		var input_event := create_shortcut_input_even(value)
		prints("Shortcut changed: '%s' to '%s'" % [GdUnitShortcut.ShortCut.keys()[shortcut], input_event.as_text()])
		var action := get_shortcut_action(shortcut)
		if action != null:
			action.update_shortcut(input_event)
		else:
			register_shortcut(shortcut, input_event)
	if property.name() == GdUnitSettings.TEST_DISCOVER_ENABLED:
		var timer :SceneTreeTimer = (Engine.get_main_loop() as SceneTree).create_timer(3)
		@warning_ignore("return_value_discarded")
		timer.timeout.connect(cmd_discover_tests)


################################################################################
# Network stuff
################################################################################
func _on_client_connected(client_id: int) -> void:
	_client_id = client_id


func _on_client_disconnected(client_id: int) -> void:
	# only stops is not in debug mode running and the current client
	if not _running_debug_mode and _client_id == client_id:
		cmd_stop(client_id)
	_client_id = -1
