@tool
class_name GdUnitSettings
extends RefCounted


const MAIN_CATEGORY = "gdunit4"
# Common Settings
const COMMON_SETTINGS = MAIN_CATEGORY + "/settings"

const GROUP_COMMON = COMMON_SETTINGS + "/common"
const UPDATE_NOTIFICATION_ENABLED = GROUP_COMMON + "/update_notification_enabled"
const SERVER_TIMEOUT = GROUP_COMMON + "/server_connection_timeout_minutes"

const GROUP_TEST = COMMON_SETTINGS + "/test"
const TEST_TIMEOUT = GROUP_TEST + "/test_timeout_seconds"
const TEST_LOOKUP_FOLDER = GROUP_TEST + "/test_lookup_folder"
const TEST_SUITE_NAMING_CONVENTION = GROUP_TEST + "/test_suite_naming_convention"
const TEST_DISCOVER_ENABLED = GROUP_TEST + "/test_discovery"
const TEST_FLAKY_CHECK = GROUP_TEST + "/flaky_check_enable"
const TEST_FLAKY_MAX_RETRIES = GROUP_TEST + "/flaky_max_retries"


# Report Setiings
const REPORT_SETTINGS = MAIN_CATEGORY + "/report"
const GROUP_GODOT = REPORT_SETTINGS + "/godot"
const REPORT_PUSH_ERRORS = GROUP_GODOT + "/push_error"
const REPORT_SCRIPT_ERRORS = GROUP_GODOT + "/script_error"
const REPORT_ORPHANS  = REPORT_SETTINGS + "/verbose_orphans"
const GROUP_ASSERT = REPORT_SETTINGS + "/assert"
const REPORT_ASSERT_WARNINGS = GROUP_ASSERT + "/verbose_warnings"
const REPORT_ASSERT_ERRORS   = GROUP_ASSERT + "/verbose_errors"
const REPORT_ASSERT_STRICT_NUMBER_TYPE_COMPARE = GROUP_ASSERT + "/strict_number_type_compare"

# Godot debug stdout/logging settings
const CATEGORY_LOGGING := "debug/file_logging/"
const STDOUT_ENABLE_TO_FILE = CATEGORY_LOGGING + "enable_file_logging"
const STDOUT_WITE_TO_FILE = CATEGORY_LOGGING + "log_path"


# GdUnit Templates
const TEMPLATES = MAIN_CATEGORY + "/templates"
const TEMPLATES_TS = TEMPLATES + "/testsuite"
const TEMPLATE_TS_GD = TEMPLATES_TS + "/GDScript"
const TEMPLATE_TS_CS = TEMPLATES_TS + "/CSharpScript"


# UI Setiings
const UI_SETTINGS = MAIN_CATEGORY + "/ui"
const GROUP_UI_INSPECTOR = UI_SETTINGS + "/inspector"
const INSPECTOR_NODE_COLLAPSE = GROUP_UI_INSPECTOR + "/node_collapse"
const INSPECTOR_TREE_VIEW_MODE = GROUP_UI_INSPECTOR + "/tree_view_mode"
const INSPECTOR_TREE_SORT_MODE = GROUP_UI_INSPECTOR + "/tree_sort_mode"


# Shortcut Setiings
const SHORTCUT_SETTINGS = MAIN_CATEGORY + "/Shortcuts"
const GROUP_SHORTCUT_INSPECTOR = SHORTCUT_SETTINGS + "/inspector"
const SHORTCUT_INSPECTOR_RERUN_TEST = GROUP_SHORTCUT_INSPECTOR + "/rerun_test"
const SHORTCUT_INSPECTOR_RERUN_TEST_DEBUG = GROUP_SHORTCUT_INSPECTOR + "/rerun_test_debug"
const SHORTCUT_INSPECTOR_RUN_TEST_OVERALL = GROUP_SHORTCUT_INSPECTOR + "/run_test_overall"
const SHORTCUT_INSPECTOR_RUN_TEST_STOP = GROUP_SHORTCUT_INSPECTOR + "/run_test_stop"

const GROUP_SHORTCUT_EDITOR = SHORTCUT_SETTINGS + "/editor"
const SHORTCUT_EDITOR_RUN_TEST = GROUP_SHORTCUT_EDITOR + "/run_test"
const SHORTCUT_EDITOR_RUN_TEST_DEBUG = GROUP_SHORTCUT_EDITOR + "/run_test_debug"
const SHORTCUT_EDITOR_CREATE_TEST = GROUP_SHORTCUT_EDITOR + "/create_test"

const GROUP_SHORTCUT_FILESYSTEM = SHORTCUT_SETTINGS + "/filesystem"
const SHORTCUT_FILESYSTEM_RUN_TEST = GROUP_SHORTCUT_FILESYSTEM + "/run_test"
const SHORTCUT_FILESYSTEM_RUN_TEST_DEBUG = GROUP_SHORTCUT_FILESYSTEM + "/run_test_debug"


# Toolbar Setiings
const GROUP_UI_TOOLBAR = UI_SETTINGS + "/toolbar"
const INSPECTOR_TOOLBAR_BUTTON_RUN_OVERALL = GROUP_UI_TOOLBAR + "/run_overall"

# defaults
# server connection timeout in minutes
const DEFAULT_SERVER_TIMEOUT :int = 30
# test case runtime timeout in seconds
const DEFAULT_TEST_TIMEOUT :int = 60*5
# the folder to create new test-suites
const DEFAULT_TEST_LOOKUP_FOLDER := "test"

# help texts
const HELP_TEST_LOOKUP_FOLDER := "Subfolder where test suites are located (or empty to use source folder directly)"

enum NAMING_CONVENTIONS {
	AUTO_DETECT,
	SNAKE_CASE,
	PASCAL_CASE,
}


const _VALUE_SET_SEPARATOR = "\f" # ASCII Form-feed character (AKA page break)


static func setup() -> void:
	create_property_if_need(UPDATE_NOTIFICATION_ENABLED, true, "Show notification if new gdUnit4 version is found")
	# test settings
	create_property_if_need(SERVER_TIMEOUT, DEFAULT_SERVER_TIMEOUT, "Server connection timeout in minutes")
	create_property_if_need(TEST_TIMEOUT, DEFAULT_TEST_TIMEOUT, "Test case runtime timeout in seconds")
	create_property_if_need(TEST_LOOKUP_FOLDER, DEFAULT_TEST_LOOKUP_FOLDER, HELP_TEST_LOOKUP_FOLDER)
	create_property_if_need(TEST_SUITE_NAMING_CONVENTION, NAMING_CONVENTIONS.AUTO_DETECT, "Naming convention to use when generating testsuites", NAMING_CONVENTIONS.keys())
	create_property_if_need(TEST_DISCOVER_ENABLED, false, "Automatically detect new tests in test lookup folders at runtime")
	create_property_if_need(TEST_FLAKY_CHECK, false, "Rerun tests on failure and mark them as FLAKY")
	create_property_if_need(TEST_FLAKY_MAX_RETRIES, 3, "Sets the number of retries for rerunning a flaky test")
	# report settings
	create_property_if_need(REPORT_PUSH_ERRORS, false, "Report push_error() as failure")
	create_property_if_need(REPORT_SCRIPT_ERRORS, true, "Report script errors as failure")
	create_property_if_need(REPORT_ORPHANS, true, "Report orphaned nodes after tests finish")
	create_property_if_need(REPORT_ASSERT_ERRORS, true, "Report assertion failures as errors")
	create_property_if_need(REPORT_ASSERT_WARNINGS, true, "Report assertion failures as warnings")
	create_property_if_need(REPORT_ASSERT_STRICT_NUMBER_TYPE_COMPARE, true, "Compare number values strictly by type (real vs int)")
	# inspector
	create_property_if_need(INSPECTOR_NODE_COLLAPSE, true,
		"Close testsuite node after a successful test run.")
	create_property_if_need(INSPECTOR_TREE_VIEW_MODE, GdUnitInspectorTreeConstants.TREE_VIEW_MODE.TREE,
		"Inspector panel presentation mode", GdUnitInspectorTreeConstants.TREE_VIEW_MODE.keys())
	create_property_if_need(INSPECTOR_TREE_SORT_MODE, GdUnitInspectorTreeConstants.SORT_MODE.UNSORTED,
		"Inspector panel sorting mode", GdUnitInspectorTreeConstants.SORT_MODE.keys())
	create_property_if_need(INSPECTOR_TOOLBAR_BUTTON_RUN_OVERALL, false,
		"Show 'Run overall Tests' button in the inspector toolbar")
	create_property_if_need(TEMPLATE_TS_GD, GdUnitTestSuiteTemplate.default_GD_template(), "Test suite template to use")
	create_shortcut_properties_if_need()
	migrate_properties()


static func migrate_properties() -> void:
	var TEST_ROOT_FOLDER := "gdunit4/settings/test/test_root_folder"
	if get_property(TEST_ROOT_FOLDER) != null:
		migrate_property(TEST_ROOT_FOLDER,\
			TEST_LOOKUP_FOLDER,\
			DEFAULT_TEST_LOOKUP_FOLDER,\
			HELP_TEST_LOOKUP_FOLDER,\
			func(value :Variant) -> String: return DEFAULT_TEST_LOOKUP_FOLDER if value == null else value)


static func create_shortcut_properties_if_need() -> void:
	# inspector
	create_property_if_need(SHORTCUT_INSPECTOR_RERUN_TEST, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.RERUN_TESTS), "Rerun the most recently executed tests")
	create_property_if_need(SHORTCUT_INSPECTOR_RERUN_TEST_DEBUG, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.RERUN_TESTS_DEBUG), "Rerun the most recently executed tests (Debug mode)")
	create_property_if_need(SHORTCUT_INSPECTOR_RUN_TEST_OVERALL, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.RUN_TESTS_OVERALL), "Runs all tests (Debug mode)")
	create_property_if_need(SHORTCUT_INSPECTOR_RUN_TEST_STOP, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.STOP_TEST_RUN), "Stop the current test execution")
	# script editor
	create_property_if_need(SHORTCUT_EDITOR_RUN_TEST, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.RUN_TESTCASE), "Run the currently selected test")
	create_property_if_need(SHORTCUT_EDITOR_RUN_TEST_DEBUG, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.RUN_TESTCASE_DEBUG), "Run the currently selected test (Debug mode).")
	create_property_if_need(SHORTCUT_EDITOR_CREATE_TEST, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.CREATE_TEST), "Create a new test case for the currently selected function")
	# filesystem
	create_property_if_need(SHORTCUT_FILESYSTEM_RUN_TEST, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.NONE), "Run all test suites in the selected folder or file")
	create_property_if_need(SHORTCUT_FILESYSTEM_RUN_TEST_DEBUG, GdUnitShortcut.default_keys(GdUnitShortcut.ShortCut.NONE), "Run all test suites in the selected folder or file (Debug)")


static func create_property_if_need(name :String, default :Variant, help :="", value_set := PackedStringArray()) -> void:
	if not ProjectSettings.has_setting(name):
		#prints("GdUnit4: Set inital settings '%s' to '%s'." % [name, str(default)])
		ProjectSettings.set_setting(name, default)

	ProjectSettings.set_initial_value(name, default)
	help = help if value_set.is_empty() else "%s%s%s" % [help, _VALUE_SET_SEPARATOR, value_set]
	set_help(name, default, help)


static func set_help(property_name :String, value :Variant, help :String) -> void:
	ProjectSettings.add_property_info({
		"name": property_name,
		"type": typeof(value),
		"hint": PROPERTY_HINT_TYPE_STRING,
		"hint_string": help
	})


static func get_setting(name :String, default :Variant) -> Variant:
	if ProjectSettings.has_setting(name):
		return ProjectSettings.get_setting(name)
	return default


static func is_update_notification_enabled() -> bool:
	if ProjectSettings.has_setting(UPDATE_NOTIFICATION_ENABLED):
		return ProjectSettings.get_setting(UPDATE_NOTIFICATION_ENABLED)
	return false


static func set_update_notification(enable :bool) -> void:
	ProjectSettings.set_setting(UPDATE_NOTIFICATION_ENABLED, enable)
	@warning_ignore("return_value_discarded")
	ProjectSettings.save()


static func get_log_path() -> String:
	return ProjectSettings.get_setting(STDOUT_WITE_TO_FILE)


static func set_log_path(path :String) -> void:
	ProjectSettings.set_setting(STDOUT_ENABLE_TO_FILE, true)
	ProjectSettings.set_setting(STDOUT_WITE_TO_FILE, path)
	@warning_ignore("return_value_discarded")
	ProjectSettings.save()


static func set_inspector_tree_sort_mode(sort_mode: GdUnitInspectorTreeConstants.SORT_MODE) -> void:
	var property := get_property(INSPECTOR_TREE_SORT_MODE)
	property.set_value(sort_mode)
	update_property(property)


static func get_inspector_tree_sort_mode() -> GdUnitInspectorTreeConstants.SORT_MODE:
	var property := get_property(INSPECTOR_TREE_SORT_MODE)
	return property.value() if property != null else GdUnitInspectorTreeConstants.SORT_MODE.UNSORTED


static func set_inspector_tree_view_mode(tree_view_mode: GdUnitInspectorTreeConstants.TREE_VIEW_MODE) -> void:
	var property := get_property(INSPECTOR_TREE_VIEW_MODE)
	property.set_value(tree_view_mode)
	update_property(property)


static func get_inspector_tree_view_mode() -> GdUnitInspectorTreeConstants.TREE_VIEW_MODE:
	var property := get_property(INSPECTOR_TREE_VIEW_MODE)
	return property.value() if property != null else GdUnitInspectorTreeConstants.TREE_VIEW_MODE.TREE


# the configured server connection timeout in ms
static func server_timeout() -> int:
	return get_setting(SERVER_TIMEOUT, DEFAULT_SERVER_TIMEOUT) * 60 * 1000


# the configured test case timeout in ms
static func test_timeout() -> int:
	return get_setting(TEST_TIMEOUT, DEFAULT_TEST_TIMEOUT) * 1000


# the root folder to store/generate test-suites
static func test_root_folder() -> String:
	return get_setting(TEST_LOOKUP_FOLDER, DEFAULT_TEST_LOOKUP_FOLDER)


static func is_verbose_assert_warnings() -> bool:
	return get_setting(REPORT_ASSERT_WARNINGS, true)


static func is_verbose_assert_errors() -> bool:
	return get_setting(REPORT_ASSERT_ERRORS, true)


static func is_verbose_orphans() -> bool:
	return get_setting(REPORT_ORPHANS, true)


static func is_strict_number_type_compare() -> bool:
	return get_setting(REPORT_ASSERT_STRICT_NUMBER_TYPE_COMPARE, true)


static func is_report_push_errors() -> bool:
	return get_setting(REPORT_PUSH_ERRORS, false)


static func is_report_script_errors() -> bool:
	return get_setting(REPORT_SCRIPT_ERRORS, true)


static func is_inspector_node_collapse() -> bool:
	return get_setting(INSPECTOR_NODE_COLLAPSE, true)


static func is_inspector_toolbar_button_show() -> bool:
	return get_setting(INSPECTOR_TOOLBAR_BUTTON_RUN_OVERALL, true)


static func is_test_discover_enabled() -> bool:
	return get_setting(TEST_DISCOVER_ENABLED, false)


static func is_test_flaky_check_enabled() -> bool:
	return get_setting(TEST_FLAKY_CHECK, false)


static func get_flaky_max_retries() -> int:
	return get_setting(TEST_FLAKY_MAX_RETRIES, 3)


static func set_test_discover_enabled(enable :bool) -> void:
	var property := get_property(TEST_DISCOVER_ENABLED)
	property.set_value(enable)
	update_property(property)


static func is_log_enabled() -> bool:
	return ProjectSettings.get_setting(STDOUT_ENABLE_TO_FILE)


static func list_settings(category: String) -> Array[GdUnitProperty]:
	var settings: Array[GdUnitProperty] = []
	for property in ProjectSettings.get_property_list():
		var property_name :String = property["name"]
		if property_name.begins_with(category):
			settings.append(build_property(property_name, property))
	return settings


static func extract_value_set_from_help(value :String) -> PackedStringArray:
	var split_value := value.split(_VALUE_SET_SEPARATOR)
	if not split_value.size() > 1:
		return PackedStringArray()

	var regex := RegEx.new()
	@warning_ignore("return_value_discarded")
	regex.compile("\\[(.+)\\]")
	var matches := regex.search_all(split_value[1])
	if matches.is_empty():
		return PackedStringArray()
	var values: String = matches[0].get_string(1)
	return values.replacen(" ", "").replacen("\"", "").split(",", false)


static func extract_help_text(value :String) -> String:
	return value.split(_VALUE_SET_SEPARATOR)[0]


static func update_property(property :GdUnitProperty) -> Variant:
	var current_value :Variant = ProjectSettings.get_setting(property.name())
	if current_value != property.value():
		var error :Variant = validate_property_value(property)
		if error != null:
			return error
		ProjectSettings.set_setting(property.name(), property.value())
		GdUnitSignals.instance().gdunit_settings_changed.emit(property)
		_save_settings()
	return null


static func reset_property(property :GdUnitProperty) -> void:
	ProjectSettings.set_setting(property.name(), property.default())
	GdUnitSignals.instance().gdunit_settings_changed.emit(property)
	_save_settings()


static func validate_property_value(property :GdUnitProperty) -> Variant:
	match property.name():
		TEST_LOOKUP_FOLDER:
			return validate_lookup_folder(property.value_as_string())
		_: return null


static func validate_lookup_folder(value :String) -> Variant:
	if value.is_empty() or value == "/":
		return null
	if value.contains("res:"):
		return "Test Lookup Folder: do not allowed to contains 'res://'"
	if not value.is_valid_filename():
		return "Test Lookup Folder: contains invalid characters! e.g (: / \\ ? * \" | % < >)"
	return null


static func save_property(name :String, value :Variant) -> void:
	ProjectSettings.set_setting(name, value)
	_save_settings()


static func _save_settings() -> void:
	var err := ProjectSettings.save()
	if err != OK:
		push_error("Save GdUnit4 settings failed : %s" % error_string(err))
		return


static func has_property(name :String) -> bool:
	return ProjectSettings.get_property_list().any(func(property :Dictionary) -> bool: return property["name"] == name)


static func get_property(name :String) -> GdUnitProperty:
	for property in ProjectSettings.get_property_list():
		var property_name :String = property["name"]
		if property_name == name:
			return build_property(name, property)
	return null


static func build_property(property_name: String, property: Dictionary) -> GdUnitProperty:
	var value: Variant = ProjectSettings.get_setting(property_name)
	var value_type: int = property["type"]
	var default: Variant = ProjectSettings.property_get_revert(property_name)
	var help: String = property["hint_string"]
	var value_set := extract_value_set_from_help(help)
	return GdUnitProperty.new(property_name, value_type, value, default, extract_help_text(help), value_set)


static func migrate_property(old_property :String, new_property :String, default_value :Variant, help :String, converter := Callable()) -> void:
	var property := get_property(old_property)
	if property == null:
		prints("Migration not possible, property '%s' not found" % old_property)
		return
	var value :Variant = converter.call(property.value()) if converter.is_valid() else property.value()
	ProjectSettings.set_setting(new_property, value)
	ProjectSettings.set_initial_value(new_property, default_value)
	set_help(new_property, value, help)
	ProjectSettings.clear(old_property)
	prints("Successfully migrated property '%s' -> '%s' value: %s" % [old_property, new_property, value])


static func dump_to_tmp() -> void:
	@warning_ignore("return_value_discarded")
	ProjectSettings.save_custom("user://project_settings.godot")


static func restore_dump_from_tmp() -> void:
	@warning_ignore("return_value_discarded")
	DirAccess.copy_absolute("user://project_settings.godot", "res://project.godot")
