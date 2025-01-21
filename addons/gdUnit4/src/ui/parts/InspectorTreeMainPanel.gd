@tool
extends VSplitContainer

signal run_testcase(test_suite_resource_path: String, test_case: String, test_param_index: int, run_debug: bool)
signal run_testsuite()

const CONTEXT_MENU_RUN_ID = 0
const CONTEXT_MENU_DEBUG_ID = 1
const CONTEXT_MENU_COLLAPSE_ALL = 3
const CONTEXT_MENU_EXPAND_ALL = 4


@onready var _tree: Tree = $Panel/Tree
@onready var _report_list: Node = $report/ScrollContainer/list
@onready var _report_template: RichTextLabel = $report/report_template
@onready var _context_menu: PopupMenu = $contextMenu
@onready var _discover_hint: Control = %discover_hint
@onready var _spinner: Button = %spinner

# loading tree icons
@onready var ICON_SPINNER := GdUnitUiTools.get_spinner()
@onready var ICON_FOLDER := GdUnitUiTools.get_icon("Folder")
# gdscript icons
@onready var ICON_GDSCRIPT_TEST_DEFAULT := GdUnitUiTools.get_icon("GDScript", Color.LIGHT_GRAY)
@onready var ICON_GDSCRIPT_TEST_SUCCESS := GdUnitUiTools.get_GDScript_icon("StatusSuccess", Color.DARK_GREEN)
@onready var ICON_GDSCRIPT_TEST_FLAKY := GdUnitUiTools.get_GDScript_icon("CheckBox", Color.GREEN_YELLOW)
@onready var ICON_GDSCRIPT_TEST_FAILED := GdUnitUiTools.get_GDScript_icon("StatusError", Color.SKY_BLUE)
@onready var ICON_GDSCRIPT_TEST_ERROR := GdUnitUiTools.get_GDScript_icon("StatusError", Color.DARK_RED)
@onready var ICON_GDSCRIPT_TEST_SUCCESS_ORPHAN := GdUnitUiTools.get_GDScript_icon("Unlinked", Color.DARK_GREEN)
@onready var ICON_GDSCRIPT_TEST_FAILED_ORPHAN := GdUnitUiTools.get_GDScript_icon("Unlinked", Color.SKY_BLUE)
@onready var ICON_GDSCRIPT_TEST_ERRORS_ORPHAN := GdUnitUiTools.get_GDScript_icon("Unlinked", Color.DARK_RED)
# csharp script icons
@onready var ICON_CSSCRIPT_TEST_DEFAULT := GdUnitUiTools.get_icon("CSharpScript", Color.LIGHT_GRAY)
@onready var ICON_CSSCRIPT_TEST_SUCCESS := GdUnitUiTools.get_CSharpScript_icon("StatusSuccess", Color.DARK_GREEN)
@onready var ICON_CSSCRIPT_TEST_FAILED := GdUnitUiTools.get_CSharpScript_icon("StatusError", Color.SKY_BLUE)
@onready var ICON_CSSCRIPT_TEST_ERROR := GdUnitUiTools.get_CSharpScript_icon("StatusError", Color.DARK_RED)
@onready var ICON_CSSCRIPT_TEST_SUCCESS_ORPHAN := GdUnitUiTools.get_CSharpScript_icon("Unlinked", Color.DARK_GREEN)
@onready var ICON_CSSCRIPT_TEST_FAILED_ORPHAN := GdUnitUiTools.get_CSharpScript_icon("Unlinked", Color.SKY_BLUE)
@onready var ICON_CSSCRIPT_TEST_ERRORS_ORPHAN := GdUnitUiTools.get_CSharpScript_icon("Unlinked", Color.DARK_RED)


enum GdUnitType {
	FOLDER,
	TEST_SUITE,
	TEST_CASE,
	TEST_CASE_PARAMETERIZED
}


enum STATE {
	INITIAL,
	RUNNING,
	SUCCESS,
	WARNING,
	FLAKY,
	FAILED,
	ERROR,
	ABORDED,
	SKIPPED
}

const META_GDUNIT_ORIGINAL_INDEX = "gdunit_original_index"
const META_GDUNIT_NAME := "gdUnit_name"
const META_GDUNIT_STATE := "gdUnit_state"
const META_GDUNIT_TYPE := "gdUnit_type"
const META_GDUNIT_TOTAL_TESTS := "gdUnit_suite_total_tests"
const META_GDUNIT_SUCCESS_TESTS := "gdUnit_suite_success_tests"
const META_GDUNIT_REPORT := "gdUnit_report"
const META_GDUNIT_ORPHAN := "gdUnit_orphan"
const META_GDUNIT_EXECUTION_TIME := "gdUnit_execution_time"
const META_RESOURCE_PATH := "resource_path"
const META_LINE_NUMBER := "line_number"
const META_SCRIPT_PATH := "script_path"
const META_TEST_PARAM_INDEX := "test_param_index"

var _tree_root: TreeItem
var _item_hash := Dictionary()
var _tree_view_mode_flat := GdUnitSettings.get_inspector_tree_view_mode() == GdUnitInspectorTreeConstants.TREE_VIEW_MODE.FLAT


func _build_cache_key(resource_path: String, test_name: String) -> Array:
	return [resource_path, test_name]


func get_tree_item(resource_path: String, item_name: String) -> TreeItem:
	var key := _build_cache_key(resource_path, item_name)
	return _item_hash.get(key, null)


func remove_tree_item(resource_path: String, item_name: String) -> bool:
	var key := _build_cache_key(resource_path, item_name)
	var item :TreeItem= _item_hash.get(key, null)
	if item:
		item.get_parent().remove_child(item)
		item.free()
		return _item_hash.erase(key)
	return false


func add_tree_item_to_cache(resource_path: String, test_name: String, item: TreeItem) -> void:
	var key := _build_cache_key(resource_path, test_name)
	_item_hash[key] = item


func clear_tree_item_cache() -> void:
	_item_hash.clear()


func _find_by_resource_path(current: TreeItem, resource_path: String) -> TreeItem:
	for item in current.get_children():
		if item.get_meta(META_RESOURCE_PATH) == resource_path:
			return item
	return null


func _find_first_item_by_state(parent: TreeItem, item_state: STATE, reverse := false) -> TreeItem:
	var itmes := parent.get_children()
	if reverse:
		itmes.reverse()
	for item in itmes:
		if is_test_case(item) and (is_item_state(item, item_state)):
			return item
		var failure_item := _find_first_item_by_state(item, item_state, reverse)
		if failure_item != null:
			return failure_item
	return null


func _find_last_item_by_state(parent: TreeItem, item_state: STATE) -> TreeItem:
	return _find_first_item_by_state(parent, item_state, true)


func _find_item_by_state(current: TreeItem, item_state: STATE, prev := false) -> TreeItem:
	var next := current.get_prev_in_tree() if prev else current.get_next_in_tree()
	if next == null or next == _tree_root:
		return null
	if is_test_case(next) and is_item_state(next, item_state):
		return next
	return _find_item_by_state(next, item_state, prev)


func is_item_state(item: TreeItem, item_state: STATE) -> bool:
	return item.has_meta(META_GDUNIT_STATE) and item.get_meta(META_GDUNIT_STATE) == item_state


func is_state_running(item: TreeItem) -> bool:
	return is_item_state(item, STATE.RUNNING)


func is_state_success(item: TreeItem) -> bool:
	return is_item_state(item, STATE.SUCCESS)


func is_state_warning(item: TreeItem) -> bool:
	return is_item_state(item, STATE.WARNING)


func is_state_failed(item: TreeItem) -> bool:
	return is_item_state(item, STATE.FAILED)


func is_state_error(item: TreeItem) -> bool:
	return is_item_state(item, STATE.ERROR) or is_item_state(item, STATE.ABORDED)


func is_item_state_orphan(item: TreeItem) -> bool:
	return item.has_meta(META_GDUNIT_ORPHAN)


func is_test_suite(item: TreeItem) -> bool:
	return item.has_meta(META_GDUNIT_TYPE) and item.get_meta(META_GDUNIT_TYPE) == GdUnitType.TEST_SUITE


func is_test_case(item: TreeItem) -> bool:
	return item.has_meta(META_GDUNIT_TYPE) and item.get_meta(META_GDUNIT_TYPE) == GdUnitType.TEST_CASE


func is_folder(item: TreeItem) -> bool:
	return item.has_meta(META_GDUNIT_TYPE) and item.get_meta(META_GDUNIT_TYPE) == GdUnitType.FOLDER


@warning_ignore("return_value_discarded")
func _ready() -> void:
	_context_menu.set_item_icon(CONTEXT_MENU_RUN_ID, GdUnitUiTools.get_icon("Play"))
	_context_menu.set_item_icon(CONTEXT_MENU_DEBUG_ID, GdUnitUiTools.get_icon("PlayStart"))
	_context_menu.set_item_icon(CONTEXT_MENU_EXPAND_ALL, GdUnitUiTools.get_icon("ExpandTree"))
	_context_menu.set_item_icon(CONTEXT_MENU_COLLAPSE_ALL, GdUnitUiTools.get_icon("CollapseTree"))
	# do colorize the icons
	#for index in _context_menu.item_count:
	#	_context_menu.set_item_icon_modulate(index, Color.MEDIUM_PURPLE)

	_spinner.icon = GdUnitUiTools.get_spinner()
	init_tree()
	GdUnitSignals.instance().gdunit_settings_changed.connect(_on_settings_changed)
	GdUnitSignals.instance().gdunit_add_test_suite.connect(do_add_test_suite)
	GdUnitSignals.instance().gdunit_event.connect(_on_gdunit_event)
	var command_handler := GdUnitCommandHandler.instance()
	command_handler.gdunit_runner_start.connect(_on_gdunit_runner_start)
	command_handler.gdunit_runner_stop.connect(_on_gdunit_runner_stop)


# we need current to manually redraw bacause of the animation bug
# https://github.com/godotengine/godot/issues/69330
func _process(_delta: float) -> void:
	if is_visible_in_tree():
		queue_redraw()


func init_tree() -> void:
	cleanup_tree()
	_tree.set_hide_root(true)
	_tree.ensure_cursor_is_visible()
	_tree.set_allow_reselect(true)
	_tree.set_allow_rmb_select(true)
	_tree.set_columns(2)
	_tree.set_column_clip_content(0, true)
	_tree.set_column_expand_ratio(0, 1)
	_tree.set_column_custom_minimum_width(0, 240)
	_tree.set_column_expand_ratio(1, 0)
	_tree.set_column_custom_minimum_width(1, 100)
	_tree_root = _tree.create_item()
	# fix tree icon scaling
	var scale_factor := EditorInterface.get_editor_scale() if Engine.is_editor_hint() else 1.0
	_tree.set("theme_override_constants/icon_max_width", 16 * scale_factor)


func cleanup_tree() -> void:
	clear_reports()
	clear_tree_item_cache()
	if not _tree_root:
		return
	_free_recursive()
	_tree.clear()


func _free_recursive(items:=_tree_root.get_children()) -> void:
	for item in items:
		_free_recursive(item.get_children())
		item.call_deferred("free")


func sort_tree_items(parent :TreeItem) -> void:
	parent.visible = false
	var items := parent.get_children()

	# do sort by selected sort mode
	match GdUnitSettings.get_inspector_tree_sort_mode():
		GdUnitInspectorTreeConstants.SORT_MODE.UNSORTED:
			items.sort_custom(sort_items_by_original_index)

		GdUnitInspectorTreeConstants.SORT_MODE.NAME_ASCENDING:
			items.sort_custom(sort_items_by_name.bind(true))

		GdUnitInspectorTreeConstants.SORT_MODE.NAME_DESCENDING:
			items.sort_custom(sort_items_by_name.bind(false))

		GdUnitInspectorTreeConstants.SORT_MODE.EXECUTION_TIME:
			items.sort_custom(sort_items_by_execution_time)

	for item in items:
		parent.remove_child(item)
		parent.add_child(item)
		if item.get_child_count() > 0:
			sort_tree_items(item)
	parent.visible = true
	_tree.queue_redraw()


func sort_items_by_name(a: TreeItem, b: TreeItem, ascending: bool) -> bool:
	var type_a: GdUnitType = a.get_meta(META_GDUNIT_TYPE)
	var type_b: GdUnitType = b.get_meta(META_GDUNIT_TYPE)
	 # Compare types first
	if type_a != type_b:
		return type_a == GdUnitType.FOLDER
	var name_a :String = a.get_meta(META_GDUNIT_NAME)
	var name_b :String = b.get_meta(META_GDUNIT_NAME)
	return name_a.naturalnocasecmp_to(name_b) < 0 if ascending else name_a.naturalnocasecmp_to(name_b) > 0


func sort_items_by_execution_time(a: TreeItem, b: TreeItem) -> bool:
	var type_a: GdUnitType = a.get_meta(META_GDUNIT_TYPE)
	var type_b: GdUnitType = b.get_meta(META_GDUNIT_TYPE)
	 # Compare types first
	if type_a != type_b:
		return type_a == GdUnitType.FOLDER
	var execution_time_a :int = a.get_meta(META_GDUNIT_EXECUTION_TIME)
	var execution_time_b :int = b.get_meta(META_GDUNIT_EXECUTION_TIME)
	# if has same execution time sort by name
	if execution_time_a == execution_time_b:
		var name_a :String = a.get_meta(META_GDUNIT_NAME)
		var name_b :String = b.get_meta(META_GDUNIT_NAME)
		return name_a.naturalnocasecmp_to(name_b) > 0
	return execution_time_a > execution_time_b


func sort_items_by_original_index(a: TreeItem, b: TreeItem) -> bool:
	var type_a: GdUnitType = a.get_meta(META_GDUNIT_TYPE)
	var type_b: GdUnitType = b.get_meta(META_GDUNIT_TYPE)
	if type_a != type_b:
		return type_a == GdUnitType.FOLDER
	var index_a :int = a.get_meta(META_GDUNIT_ORIGINAL_INDEX)
	var index_b :int = b.get_meta(META_GDUNIT_ORIGINAL_INDEX)
	return index_a < index_b


func reset_tree_state(parent: TreeItem) -> void:
	for item in parent.get_children():
		set_state_initial(item)
		reset_tree_state(item)


func select_item(item: TreeItem) -> TreeItem:
	if item != null:
		# enshure the parent is collapsed
		do_collapse_parent(item)
		item.select(0)
		_tree.ensure_cursor_is_visible()
		_tree.scroll_to_item(item, true)
	return item


func do_collapse_parent(item: TreeItem) -> void:
	if item != null:
		item.collapsed = false
		do_collapse_parent(item.get_parent())


func do_collapse_all(collapse: bool, parent := _tree_root) -> void:
	for item in parent.get_children():
		item.collapsed = collapse
		if not collapse:
			do_collapse_all(collapse, item)


func set_state_initial(item: TreeItem) -> void:
	item.set_custom_color(0, Color.LIGHT_GRAY)
	item.set_tooltip_text(0, "")
	item.set_text_overrun_behavior(0, TextServer.OVERRUN_TRIM_CHAR)
	item.set_expand_right(0, true)

	item.set_custom_color(1, Color.LIGHT_GRAY)
	item.set_text(1, "")
	item.set_expand_right(1, true)
	item.set_tooltip_text(1, "")

	item.set_meta(META_GDUNIT_STATE, STATE.INITIAL)
	item.set_meta(META_GDUNIT_SUCCESS_TESTS, 0)
	item.remove_meta(META_GDUNIT_REPORT)
	item.remove_meta(META_GDUNIT_ORPHAN)
	set_item_icon_by_state(item)
	init_item_counter(item)


func set_state_running(item: TreeItem) -> void:
	if is_state_running(item):
		return
	item.set_custom_color(0, Color.DARK_GREEN)
	item.set_custom_color(1, Color.DARK_GREEN)
	item.set_icon(0, ICON_SPINNER)
	item.set_meta(META_GDUNIT_STATE, STATE.RUNNING)
	item.collapsed = false
	var parent := item.get_parent()
	if parent != _tree_root:
		set_state_running(parent)
	# force scrolling to current test case
	@warning_ignore("return_value_discarded")
	select_item(item)


func set_state_succeded(item: TreeItem) -> void:
	item.set_custom_color(0, Color.GREEN)
	item.set_custom_color(1, Color.GREEN)
	item.set_meta(META_GDUNIT_STATE, STATE.SUCCESS)
	item.collapsed = GdUnitSettings.is_inspector_node_collapse()
	set_item_icon_by_state(item)


func set_state_flaky(item: TreeItem, event: GdUnitEvent) -> void:
	# Do not overwrite higher states
	if is_state_error(item):
		return
	var retry_count := event.statistic(GdUnitEvent.RETRY_COUNT)
	item.set_meta(META_GDUNIT_STATE, STATE.FLAKY)
	if retry_count > 1:
		item.set_text(0, "%s (%s retries)" % [
			item.get_meta(META_GDUNIT_NAME),
			retry_count])
	item.set_custom_color(0, Color.GREEN_YELLOW)
	item.set_custom_color(1, Color.GREEN_YELLOW)
	item.collapsed = false
	set_item_icon_by_state(item)


func set_state_skipped(item: TreeItem) -> void:
	item.set_meta(META_GDUNIT_STATE, STATE.SKIPPED)
	item.set_text(1, "(skipped)")
	item.set_text_alignment(1, HORIZONTAL_ALIGNMENT_RIGHT)
	item.set_custom_color(0, Color.DARK_GRAY)
	item.set_custom_color(1, Color.DARK_GRAY)
	item.collapsed = false
	set_item_icon_by_state(item)


func set_state_warnings(item: TreeItem) -> void:
	# Do not overwrite higher states
	if is_state_error(item) or is_state_failed(item):
		return
	item.set_meta(META_GDUNIT_STATE, STATE.WARNING)
	item.set_custom_color(0, Color.YELLOW)
	item.set_custom_color(1, Color.YELLOW)
	item.collapsed = false
	set_item_icon_by_state(item)


func set_state_failed(item: TreeItem, event: GdUnitEvent) -> void:
	# Do not overwrite higher states
	if is_state_error(item):
		return
	var retry_count := event.statistic(GdUnitEvent.RETRY_COUNT)
	if retry_count > 1:
		item.set_text(0, "%s (%s retries)" % [
			item.get_meta(META_GDUNIT_NAME),
			retry_count])
	item.set_meta(META_GDUNIT_STATE, STATE.FAILED)
	item.set_custom_color(0, Color.LIGHT_BLUE)
	item.set_custom_color(1, Color.LIGHT_BLUE)
	item.collapsed = false
	set_item_icon_by_state(item)


func set_state_error(item: TreeItem) -> void:
	item.set_meta(META_GDUNIT_STATE, STATE.ERROR)
	item.set_custom_color(0, Color.ORANGE_RED)
	item.set_custom_color(1, Color.ORANGE_RED)
	set_item_icon_by_state(item)
	item.collapsed = false


func set_state_aborted(item: TreeItem) -> void:
	item.set_meta(META_GDUNIT_STATE, STATE.ABORDED)
	item.set_custom_color(0, Color.ORANGE_RED)
	item.set_custom_color(1, Color.ORANGE_RED)
	item.clear_custom_bg_color(0)
	item.set_text(1, "(aborted)")
	item.set_text_alignment(1, HORIZONTAL_ALIGNMENT_RIGHT)
	set_item_icon_by_state(item)
	item.collapsed = false


func set_state_orphan(item: TreeItem, event: GdUnitEvent) -> void:
	var orphan_count := event.statistic(GdUnitEvent.ORPHAN_NODES)
	if orphan_count == 0:
		return
	if item.has_meta(META_GDUNIT_ORPHAN):
		orphan_count += item.get_meta(META_GDUNIT_ORPHAN)
	item.set_meta(META_GDUNIT_ORPHAN, orphan_count)
	if item.get_meta(META_GDUNIT_STATE) != STATE.FAILED:
		item.set_custom_color(0, Color.YELLOW)
		item.set_custom_color(1, Color.YELLOW)
	item.set_tooltip_text(0, "Total <%d> orphan nodes detected." % orphan_count)
	set_item_icon_by_state(item)


func update_state(item: TreeItem, event: GdUnitEvent, add_reports := true) -> void:
	# we do not show the root
	if item == _tree_root:
		return

	if event.is_success() and event.is_flaky():
		set_state_flaky(item, event)
	elif event.is_success():
		set_state_succeded(item)
	elif event.is_skipped():
		set_state_skipped(item)
	elif event.is_error():
		set_state_error(item)
	elif event.is_failed():
		set_state_failed(item, event)
	elif event.is_warning():
		set_state_warnings(item)
	if add_reports:
		for report in event.reports():
			add_report(item, report)
	set_state_orphan(item, event)
	if is_folder(item):
		update_state(item.get_parent(), event, false)


func add_report(item: TreeItem, report: GdUnitReport) -> void:
	var reports: Array[GdUnitReport] = []
	if item.has_meta(META_GDUNIT_REPORT):
		reports = get_item_reports(item)
	reports.append(report)
	item.set_meta(META_GDUNIT_REPORT, reports)


func abort_running(items:=_tree_root.get_children()) -> void:
	for item in items:
		if is_state_running(item):
			set_state_aborted(item)
			abort_running(item.get_children())


func select_first_failure() -> TreeItem:
	return select_item(_find_first_item_by_state(_tree_root, STATE.FAILED))


func _on_select_next_item_by_state(item_state: int) -> TreeItem:
	var current_selected := _tree.get_selected()
	# If nothing is selected, the first error is selected or the next one in the vicinity of the current selection is found
	current_selected = _find_first_item_by_state(_tree_root, item_state) if current_selected == null else _find_item_by_state(current_selected, item_state)
	# If no next failure found, then we try to select first
	if current_selected == null:
		current_selected = _find_first_item_by_state(_tree_root, item_state)
	return select_item(current_selected)


func _on_select_previous_item_by_state(item_state: int) -> TreeItem:
	var current_selected := _tree.get_selected()
	# If nothing is selected, the first error is selected or the next one in the vicinity of the current selection is found
	current_selected = _find_last_item_by_state(_tree_root, item_state) if current_selected == null else _find_item_by_state(current_selected, item_state, true)
	# If no next failure found, then we try to select first last
	if current_selected == null:
		current_selected = _find_last_item_by_state(_tree_root, item_state)
	return select_item(current_selected)


func select_first_orphan() -> void:
	for parent in _tree_root.get_children():
		if not is_state_success(parent):
			for item in parent.get_children():
				if is_item_state_orphan(item):
					parent.set_collapsed(false)
					@warning_ignore("return_value_discarded")
					select_item(item)
					return


func clear_reports() -> void:
	for child in _report_list.get_children():
		_report_list.remove_child(child)
		child.queue_free()


func show_failed_report(selected_item: TreeItem) -> void:
	clear_reports()
	if selected_item == null or not selected_item.has_meta(META_GDUNIT_REPORT):
		return
	# add new reports
	for report in get_item_reports(selected_item):
		var reportNode: RichTextLabel = _report_template.duplicate()
		_report_list.add_child(reportNode)
		reportNode.append_text(report.to_string())
		reportNode.visible = true


func update_test_suite(event: GdUnitEvent) -> void:
	var item := get_tree_item(extract_resource_path(event), event.suite_name())
	if not item:
		push_error("Internal Error: Can't find test suite %s" % event.suite_name())
		return
	if event.type() == GdUnitEvent.TESTSUITE_BEFORE:
		set_state_running(item)
		return
	if event.type() == GdUnitEvent.TESTSUITE_AFTER:
		update_item_counter(item)
		update_item_elapsed_time_counter(item, event.elapsed_time())

	update_state(item, event)
	update_state(item.get_parent(), event, false)


func update_test_case(event: GdUnitEvent) -> void:
	var item := get_tree_item(extract_resource_path(event), event.test_name())
	if not item:
		push_error("Internal Error: Can't find test case %s:%s" % [event.suite_name(), event.test_name()])
		return
	if event.type() == GdUnitEvent.TESTCASE_BEFORE:
		set_state_running(item)
		return
	if event.type() == GdUnitEvent.TESTCASE_AFTER:
		update_item_elapsed_time_counter(item, event.elapsed_time())
		if event.is_success() or event.is_warning():
			update_item_counter(item)
	update_state(item, event)


func create_tree_item(test_suite: GdUnitTestSuiteDto) -> TreeItem:
	var parent := _tree_root
	var test_root_folder := GdUnitSettings.test_root_folder()
	var resource_path := ProjectSettings.localize_path(test_suite.path())
	var test_base_path := "res://"
	var test_relative_path := resource_path
	if resource_path.contains(test_root_folder):
		var path_elements := resource_path.split(test_root_folder)
		test_base_path = path_elements[0] + "/" + test_root_folder
		test_relative_path = path_elements[1]
	test_relative_path = test_relative_path.replace("res://", "")

	if _tree_view_mode_flat:
		var element := test_relative_path.get_base_dir().trim_prefix("/")
		if element.is_empty():
			return _tree.create_item(parent)
		test_base_path += "/" + element
		parent = create_or_find_item(parent, test_base_path, element)
		return _tree.create_item(parent)

	var elements := test_relative_path.split("/")
	if elements[0] == "res://" or elements[0] == "":
		elements.remove_at(0)
	if elements.size() > 0:
		elements.remove_at(elements.size() - 1)
	for element in elements:
		test_base_path += "/" + element
		parent = create_or_find_item(parent, test_base_path, element)
	return _tree.create_item(parent)


func create_or_find_item(parent: TreeItem, resource_path: String, item_name: String) -> TreeItem:
	var item := _find_by_resource_path(parent, resource_path)
	if item != null:
		return item
	item = _tree.create_item(parent)
	item.set_meta(META_GDUNIT_ORIGINAL_INDEX, item.get_index())
	item.set_text(0, item_name)
	item.set_meta(META_GDUNIT_STATE, STATE.INITIAL)
	item.set_meta(META_GDUNIT_NAME, item_name)
	item.set_meta(META_GDUNIT_TYPE, GdUnitType.FOLDER)
	item.set_meta(META_RESOURCE_PATH, resource_path)
	item.set_meta(META_GDUNIT_TOTAL_TESTS, 0)
	item.set_meta(META_GDUNIT_EXECUTION_TIME, 0)
	set_item_icon_by_state(item)
	item.collapsed = true
	return item


func create_item(parent: TreeItem, resource_path: String, item_name: String, type: GdUnitType) -> TreeItem:
	var item := _tree.create_item(parent)
	item.set_meta(META_GDUNIT_ORIGINAL_INDEX, item.get_index())
	item.set_text(0, item_name)
	item.set_meta(META_GDUNIT_STATE, STATE.INITIAL)
	item.set_meta(META_GDUNIT_NAME, item_name)
	item.set_meta(META_GDUNIT_TYPE, type)
	item.set_meta(META_RESOURCE_PATH, resource_path)
	item.set_meta(META_GDUNIT_TOTAL_TESTS, 0)
	item.set_meta(META_GDUNIT_EXECUTION_TIME, 0)
	set_item_icon_by_state(item)
	item.collapsed = true
	return item


func set_item_icon_by_state(item :TreeItem) -> void:
	var resource_path :String = item.get_meta(META_RESOURCE_PATH)
	var state :STATE = item.get_meta(META_GDUNIT_STATE)
	var is_orphan := is_item_state_orphan(item)
	item.set_icon(0, get_icon_by_file_type(resource_path, state, is_orphan))
	if item.get_meta(META_GDUNIT_TYPE) == GdUnitType.FOLDER:
		item.set_icon_modulate(0, Color.SKY_BLUE)


func init_item_counter(item: TreeItem) -> void:
	if item.has_meta(META_GDUNIT_TOTAL_TESTS) and item.get_meta(META_GDUNIT_TOTAL_TESTS) > 0:
		item.set_text(0, "(0/%s) %s" % [
			item.get_meta(META_GDUNIT_TOTAL_TESTS),
			item.get_meta(META_GDUNIT_NAME)])
	init_folder_counter(item.get_parent())


func increment_item_counter(item: TreeItem, increment_count: int) -> void:
	if item != _tree_root and item.get_meta(META_GDUNIT_TOTAL_TESTS) != 0:
		var count: int = item.get_meta(META_GDUNIT_SUCCESS_TESTS)
		item.set_meta(META_GDUNIT_SUCCESS_TESTS, count + increment_count)
		item.set_text(0, "(%s/%s) %s" % [
			item.get_meta(META_GDUNIT_SUCCESS_TESTS),
			item.get_meta(META_GDUNIT_TOTAL_TESTS),
			item.get_meta(META_GDUNIT_NAME)])
		if is_folder(item):
			increment_item_counter(item.get_parent(), increment_count)


func init_folder_counter(item: TreeItem) -> void:
	if item == _tree_root:
		return
	var type :GdUnitType = item.get_meta(META_GDUNIT_TYPE)
	if type == GdUnitType.FOLDER:
		var count :int = item.get_children().reduce(count_tests_total, 0)
		item.set_meta(META_GDUNIT_TOTAL_TESTS, count)
		item.set_meta(META_GDUNIT_SUCCESS_TESTS, 0)
		item.set_meta(META_GDUNIT_EXECUTION_TIME, 0)
		init_item_counter(item)


func count_tests_total(accum: int, item: TreeItem) -> int:
	return accum + item.get_meta(META_GDUNIT_TOTAL_TESTS)


func update_item_counter(item: TreeItem) -> void:
	if item == _tree_root:
		return
	var type :GdUnitType = item.get_meta(META_GDUNIT_TYPE)
	match type:
		GdUnitType.TEST_CASE:
			increment_item_counter(item.get_parent(), 1)
		GdUnitType.TEST_CASE_PARAMETERIZED:
			increment_item_counter(item.get_parent(), 1)
		GdUnitType.TEST_SUITE:
			var count: int = item.get_meta(META_GDUNIT_SUCCESS_TESTS)
			increment_item_counter(item.get_parent(), count)


func update_item_elapsed_time_counter(item: TreeItem, time: int) -> void:
	item.set_text(1, "%s" % LocalTime.elapsed(time))
	item.set_text_alignment(1, HORIZONTAL_ALIGNMENT_RIGHT)
	item.set_meta(META_GDUNIT_EXECUTION_TIME, time)

	var parent := item.get_parent()
	if parent == _tree_root:
		return
	var elapsed_time :int = parent.get_meta(META_GDUNIT_EXECUTION_TIME) + time
	var type :GdUnitType = item.get_meta(META_GDUNIT_TYPE)
	match type:
		GdUnitType.TEST_CASE:
			return
		GdUnitType.TEST_SUITE:
			update_item_elapsed_time_counter(parent, elapsed_time)
		#GdUnitType.FOLDER:
		#	update_item_elapsed_time_counter(parent, elapsed_time)


func get_icon_by_file_type(path: String, state: STATE, orphans: bool) -> Texture2D:
	if path.get_extension() == "gd":
		match state:
			STATE.INITIAL:
				return ICON_GDSCRIPT_TEST_DEFAULT
			STATE.SUCCESS:
				return ICON_GDSCRIPT_TEST_SUCCESS_ORPHAN if orphans else ICON_GDSCRIPT_TEST_SUCCESS
			STATE.ERROR:
				return ICON_GDSCRIPT_TEST_ERRORS_ORPHAN if orphans else ICON_GDSCRIPT_TEST_ERROR
			STATE.FAILED:
				return ICON_GDSCRIPT_TEST_FAILED_ORPHAN if orphans else ICON_GDSCRIPT_TEST_FAILED
			STATE.WARNING:
				return ICON_GDSCRIPT_TEST_SUCCESS_ORPHAN if orphans else ICON_GDSCRIPT_TEST_DEFAULT
			STATE.FLAKY:
				return ICON_GDSCRIPT_TEST_SUCCESS_ORPHAN if orphans else ICON_GDSCRIPT_TEST_FLAKY
			_:
				return ICON_GDSCRIPT_TEST_DEFAULT
	if path.get_extension() == "cs":
		match state:
			STATE.INITIAL:
				return ICON_CSSCRIPT_TEST_DEFAULT
			STATE.SUCCESS:
				return ICON_CSSCRIPT_TEST_SUCCESS_ORPHAN if orphans else ICON_CSSCRIPT_TEST_SUCCESS
			STATE.ERROR:
				return ICON_CSSCRIPT_TEST_ERRORS_ORPHAN if orphans else ICON_CSSCRIPT_TEST_ERROR
			STATE.FAILED:
				return ICON_CSSCRIPT_TEST_FAILED_ORPHAN if orphans else ICON_CSSCRIPT_TEST_FAILED
			STATE.WARNING:
				return ICON_CSSCRIPT_TEST_SUCCESS_ORPHAN if orphans else ICON_CSSCRIPT_TEST_DEFAULT
			_:
				return ICON_CSSCRIPT_TEST_DEFAULT
	match state:
		STATE.INITIAL:
			return ICON_FOLDER
		STATE.ERROR:
			return ICON_FOLDER
		STATE.FAILED:
			return ICON_FOLDER
		_:
			return ICON_FOLDER


func discover_test_suite_added(event: GdUnitEventTestDiscoverTestSuiteAdded) -> void:
	# Check first if the test suite already exists
	var item := get_tree_item(extract_resource_path(event), event.suite_name())
	if item != null:
		return
	# Otherwise create it
	prints("Discovered test suite added: '%s' on %s" % [event.suite_name(), extract_resource_path(event)])
	do_add_test_suite(event.suite_dto())


func discover_test_added(event: GdUnitEventTestDiscoverTestAdded) -> void:
	# check if the test already exists
	var test_name := event.test_case_dto().name()
	var resource_path := extract_resource_path(event)
	var item := get_tree_item(resource_path, test_name)
	if item != null:
		return

	item = get_tree_item(resource_path, event.suite_name())
	if not item:
		push_error("Internal Error: Can't find test suite %s:%s" % [event.suite_name(), resource_path])
		return
	prints("Discovered test added: '%s' on %s" % [event.test_name(), resource_path])
	# update test case count
	var test_count :int = item.get_meta(META_GDUNIT_TOTAL_TESTS)
	item.set_meta(META_GDUNIT_TOTAL_TESTS, test_count + 1)
	init_item_counter(item)
	# add new discovered test
	add_test(item, event.test_case_dto())


func discover_test_removed(event: GdUnitEventTestDiscoverTestRemoved) -> void:
	var resource_path := extract_resource_path(event)
	prints("Discovered test removed: '%s' on %s" % [event.test_name(), resource_path])
	var item := get_tree_item(resource_path, event.test_name())
	if not item:
		push_error("Internal Error: Can't find test suite %s:%s" % [event.suite_name(), resource_path])
		return
	# update test case count on test suite
	var parent := item.get_parent()
	var test_count :int = parent.get_meta(META_GDUNIT_TOTAL_TESTS)
	parent.set_meta(META_GDUNIT_TOTAL_TESTS, test_count - 1)
	init_item_counter(parent)
	# finally remove the test
	@warning_ignore("return_value_discarded")
	remove_tree_item(resource_path, event.test_name())


func do_add_test_suite(test_suite: GdUnitTestSuiteDto) -> void:
	var item := create_tree_item(test_suite)
	var suite_name := test_suite.name()
	var resource_path := ProjectSettings.localize_path(test_suite.path())
	item.set_text(0, suite_name)
	item.set_meta(META_GDUNIT_ORIGINAL_INDEX, item.get_index())
	item.set_meta(META_GDUNIT_STATE, STATE.INITIAL)
	item.set_meta(META_GDUNIT_NAME, suite_name)
	item.set_meta(META_GDUNIT_TYPE, GdUnitType.TEST_SUITE)
	item.set_meta(META_GDUNIT_TOTAL_TESTS, test_suite.test_case_count())
	item.set_meta(META_GDUNIT_SUCCESS_TESTS, 0)
	item.set_meta(META_GDUNIT_EXECUTION_TIME, 0)
	item.set_meta(META_RESOURCE_PATH, resource_path)
	item.set_meta(META_LINE_NUMBER, 1)
	item.collapsed = true
	set_item_icon_by_state(item)
	init_item_counter(item)
	add_tree_item_to_cache(resource_path, suite_name, item)
	for test_case in test_suite.test_cases():
		add_test(item, test_case)


func add_test(parent: TreeItem, test_case: GdUnitTestCaseDto) -> void:
	var item := _tree.create_item(parent)
	var test_name := test_case.name()
	var resource_path :String = parent.get_meta(META_RESOURCE_PATH)
	var test_case_names := test_case.test_case_names()

	item.set_meta(META_GDUNIT_ORIGINAL_INDEX, item.get_index())
	item.set_text(0, test_name)
	item.set_meta(META_GDUNIT_STATE, STATE.INITIAL)
	item.set_meta(META_GDUNIT_NAME, test_name)
	item.set_meta(META_GDUNIT_TYPE, GdUnitType.TEST_CASE)
	item.set_meta(META_RESOURCE_PATH, resource_path)
	item.set_meta(META_GDUNIT_SUCCESS_TESTS, 0)
	item.set_meta(META_GDUNIT_EXECUTION_TIME, 0)
	item.set_meta(META_GDUNIT_TOTAL_TESTS, test_case_names.size())
	item.set_meta(META_SCRIPT_PATH, test_case.script_path())
	item.set_meta(META_LINE_NUMBER, test_case.line_number())
	item.set_meta(META_TEST_PARAM_INDEX, -1)
	set_item_icon_by_state(item)
	init_item_counter(item)
	add_tree_item_to_cache(resource_path, test_name, item)
	if not test_case_names.is_empty():
		add_test_cases(item, test_case_names)


func add_test_cases(parent: TreeItem, test_case_names: PackedStringArray) -> void:
	for index in test_case_names.size():
		var item := _tree.create_item(parent)
		var test_case_name := test_case_names[index]
		var resource_path :String = parent.get_meta(META_RESOURCE_PATH)
		item.set_meta(META_GDUNIT_ORIGINAL_INDEX, item.get_index())
		item.set_text(0, test_case_name)
		item.set_meta(META_GDUNIT_STATE, STATE.INITIAL)
		item.set_meta(META_GDUNIT_NAME, test_case_name)
		item.set_meta(META_GDUNIT_TOTAL_TESTS, 0)
		item.set_meta(META_GDUNIT_TYPE, GdUnitType.TEST_CASE_PARAMETERIZED)
		item.set_meta(META_GDUNIT_EXECUTION_TIME, 0)
		item.set_meta(META_RESOURCE_PATH, resource_path)
		item.set_meta(META_SCRIPT_PATH, parent.get_meta(META_SCRIPT_PATH))
		item.set_meta(META_LINE_NUMBER, parent.get_meta(META_LINE_NUMBER))
		item.set_meta(META_TEST_PARAM_INDEX, index)
		set_item_icon_by_state(item)
		add_tree_item_to_cache(resource_path, test_case_name, item)


func get_item_reports(item: TreeItem) -> Array[GdUnitReport]:
	return item.get_meta(META_GDUNIT_REPORT)


func _dump_tree_as_json(dump_name: String) -> void:
	var dict := _to_json(_tree_root)
	var file := FileAccess.open("res://%s.json" % dump_name, FileAccess.WRITE)
	file.store_string(JSON.stringify(dict, "\t"))


func _to_json(parent :TreeItem) -> Dictionary:
	var item_as_dict := GdObjects.obj2dict(parent)
	item_as_dict["TreeItem"]["childs"] = parent.get_children().map(func(item: TreeItem) -> Dictionary:
			return _to_json(item))
	return item_as_dict


func extract_resource_path(event: GdUnitEvent) -> String:
	return ProjectSettings.localize_path(event.resource_path())


################################################################################
# Tree signal receiver
################################################################################
func _on_tree_item_mouse_selected(mouse_position: Vector2, mouse_button_index: int) -> void:
	if mouse_button_index == MOUSE_BUTTON_RIGHT:
		_context_menu.position = get_screen_position() + mouse_position
		_context_menu.popup()


func _on_run_pressed(run_debug: bool) -> void:
	_context_menu.hide()
	var item: = _tree.get_selected()
	if item == null:
		print_rich("[color=GOLDENROD]Abort Testrun, no test suite selected![/color]")
		return
	if item.get_meta(META_GDUNIT_TYPE) == GdUnitType.TEST_SUITE or item.get_meta(META_GDUNIT_TYPE) == GdUnitType.FOLDER:
		var resource_path: String = item.get_meta(META_RESOURCE_PATH)
		run_testsuite.emit([resource_path], run_debug)
		return
	var parent := item.get_parent()
	var test_suite_resource_path: String = parent.get_meta(META_RESOURCE_PATH)
	var test_case: String = item.get_meta(META_GDUNIT_NAME)
	# handle parameterized test selection
	var test_param_index: int = item.get_meta(META_TEST_PARAM_INDEX)
	if test_param_index != -1:
		test_case = parent.get_meta(META_GDUNIT_NAME)
	run_testcase.emit(test_suite_resource_path, test_case, test_param_index, run_debug)


func _on_Tree_item_selected() -> void:
	# only show report checked manual item selection
	# we need to check the run mode here otherwise it will be called every selection
	if not _context_menu.is_item_disabled(CONTEXT_MENU_RUN_ID):
		var selected_item: TreeItem = _tree.get_selected()
		show_failed_report(selected_item)


# Opens the test suite
func _on_Tree_item_activated() -> void:
	var selected_item := _tree.get_selected()
	if selected_item != null and selected_item.has_meta(META_LINE_NUMBER):
		var script_path: String = (
			selected_item.get_meta(META_RESOURCE_PATH) if is_test_suite(selected_item)
			else selected_item.get_meta(META_SCRIPT_PATH)
		)
		var line_number: int = selected_item.get_meta(META_LINE_NUMBER)
		var resource: Script = load(script_path)

		if selected_item.has_meta(META_GDUNIT_REPORT):
			var reports := get_item_reports(selected_item)
			var report_line_number := reports[0].line_number()
			# if number -1 we use original stored line number of the test case
			# in non debug mode the line number is not available
			if report_line_number != -1:
				line_number = report_line_number

		EditorInterface.get_file_system_dock().navigate_to_path(script_path)
		EditorInterface.edit_script(resource, line_number)
	elif selected_item.get_meta(META_GDUNIT_TYPE) == GdUnitType.FOLDER:
		# Toggle collapse if dir
		selected_item.collapsed = not selected_item.collapsed


################################################################################
# external signal receiver
################################################################################
func _on_gdunit_runner_start() -> void:
	reset_tree_state(_tree_root)
	_context_menu.set_item_disabled(CONTEXT_MENU_RUN_ID, true)
	_context_menu.set_item_disabled(CONTEXT_MENU_DEBUG_ID, true)
	clear_reports()


func _on_gdunit_runner_stop(_client_id: int) -> void:
	_context_menu.set_item_disabled(CONTEXT_MENU_RUN_ID, false)
	_context_menu.set_item_disabled(CONTEXT_MENU_DEBUG_ID, false)
	abort_running()
	sort_tree_items(_tree_root)
	# wait until the tree redraw
	await get_tree().process_frame
	@warning_ignore("return_value_discarded")
	select_first_failure()


func _on_gdunit_event(event: GdUnitEvent) -> void:
	match event.type():
		GdUnitEvent.DISCOVER_START:
			_tree_root.visible = false
			_discover_hint.visible = true
			init_tree()

		GdUnitEvent.DISCOVER_END:
			sort_tree_items(_tree_root)
			_discover_hint.visible = false
			_tree_root.visible = true
			#_dump_tree_as_json("tree_example_discovered")

		GdUnitEvent.DISCOVER_SUITE_ADDED:
			discover_test_suite_added(event as GdUnitEventTestDiscoverTestSuiteAdded)

		GdUnitEvent.DISCOVER_TEST_ADDED:
			discover_test_added(event as GdUnitEventTestDiscoverTestAdded)

		GdUnitEvent.DISCOVER_TEST_REMOVED:
			discover_test_removed(event as GdUnitEventTestDiscoverTestRemoved)

		GdUnitEvent.INIT:
			if not GdUnitSettings.is_test_discover_enabled():
				init_tree()

		GdUnitEvent.STOP:
			sort_tree_items(_tree_root)
			#_dump_tree_as_json("tree_example")

		GdUnitEvent.TESTCASE_BEFORE:
			update_test_case(event)

		GdUnitEvent.TESTCASE_AFTER:
			update_test_case(event)

		GdUnitEvent.TESTSUITE_BEFORE:
			update_test_suite(event)

		GdUnitEvent.TESTSUITE_AFTER:
			update_test_suite(event)


func _on_context_m_index_pressed(index: int) -> void:
	match index:
		CONTEXT_MENU_DEBUG_ID:
			_on_run_pressed(true)
		CONTEXT_MENU_RUN_ID:
			_on_run_pressed(false)
		CONTEXT_MENU_EXPAND_ALL:
			do_collapse_all(false)
		CONTEXT_MENU_COLLAPSE_ALL:
			do_collapse_all(true)


func _on_settings_changed(property :GdUnitProperty) -> void:
	if property.name() == GdUnitSettings.INSPECTOR_TREE_SORT_MODE:
		sort_tree_items(_tree_root)
		# _dump_tree_as_json("tree_sorted_by_%s" % GdUnitInspectorTreeConstants.SORT_MODE.keys()[property.value()])

	if property.name() == GdUnitSettings.INSPECTOR_TREE_VIEW_MODE:
		_tree_view_mode_flat = property.value() == GdUnitInspectorTreeConstants.TREE_VIEW_MODE.FLAT
		GdUnitCommandHandler.instance().cmd_discover_tests()
