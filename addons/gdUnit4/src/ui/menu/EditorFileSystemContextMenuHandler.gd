@tool
extends Control

var _context_menus := Dictionary()
var _command_handler := GdUnitCommandHandler.instance()


func _init() -> void:
	set_name("EditorFileSystemContextMenuHandler")

	var is_test_suite := func is_visible(script: Script, is_ts: bool) -> bool:
		if script == null:
			return false
		return GdUnitTestSuiteScanner.is_test_suite(script) == is_ts
	var context_menus :Array[GdUnitContextMenuItem] = [
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.TEST_RUN, "Run Testsuites", "Play", is_test_suite.bind(true), _command_handler.command(GdUnitCommandHandler.CMD_RUN_TESTSUITE)),
		GdUnitContextMenuItem.new(GdUnitContextMenuItem.MENU_ID.TEST_DEBUG, "Debug Testsuites", "PlayStart", is_test_suite.bind(true), _command_handler.command(GdUnitCommandHandler.CMD_RUN_TESTSUITE_DEBUG)),
	]
	for menu in context_menus:
		_context_menus[menu.id] = menu
	var popup := _menu_popup()
	var file_tree := _file_tree()
	@warning_ignore("return_value_discarded")
	popup.about_to_popup.connect(on_context_menu_show.bind(popup, file_tree))
	@warning_ignore("return_value_discarded")
	popup.id_pressed.connect(on_context_menu_pressed.bind(file_tree))


func on_context_menu_show(context_menu: PopupMenu, file_tree: Tree) -> void:
	context_menu.add_separator()
	var current_index := context_menu.get_item_count()

	for menu_id: int in _context_menus.keys():
		var menu_item: GdUnitContextMenuItem = _context_menus[menu_id]

		context_menu.add_item(menu_item.name, menu_id)
		#context_menu.set_item_icon_modulate(current_index, Color.MEDIUM_PURPLE)
		context_menu.set_item_disabled(current_index, !menu_item.is_enabled(null))
		context_menu.set_item_icon(current_index, GdUnitUiTools.get_icon(menu_item.icon))
		current_index += 1


func on_context_menu_pressed(id: int, file_tree: Tree) -> void:
	if !_context_menus.has(id):
		return
	var menu_item: GdUnitContextMenuItem = _context_menus[id]
	var test_suites := collect_testsuites(menu_item, file_tree)

	menu_item.execute([test_suites])


func collect_testsuites(_menu_item: GdUnitContextMenuItem, file_tree: Tree) -> Array[Script]:
	var file_system := EditorInterface.get_resource_filesystem()
	var selected_item := file_tree.get_selected()
	var selected_test_suites: Array[Script] = []
	var suite_scaner := GdUnitTestSuiteScanner.new()

	while selected_item:
		var resource_path: String = selected_item.get_metadata(0)
		var file_type := file_system.get_file_type(resource_path)
		var is_dir := DirAccess.dir_exists_absolute(resource_path)
		if is_dir:
			selected_test_suites.append_array(suite_scaner.scan_directory(resource_path))
		elif is_dir or file_type == "GDScript" or file_type == "CSharpScript":
			# find a performant way to check if the selected item a testsuite
			var resource: Script = ResourceLoader.load(resource_path, "Script", ResourceLoader.CACHE_MODE_REUSE)
			if _menu_item.is_visible(resource):
				@warning_ignore("return_value_discarded")
				selected_test_suites.append(resource)
		selected_item = file_tree.get_next_selected(selected_item)
	return selected_test_suites


func _file_tree() -> Tree:
	return GdObjects.find_nodes_by_class(EditorInterface.get_file_system_dock(), "Tree", true)[-1]


func _menu_popup() -> PopupMenu:
	return GdObjects.find_nodes_by_class(EditorInterface.get_file_system_dock(), "PopupMenu")[-1]
