@tool
extends EditorContextMenuPlugin

var _context_menus: Array[GdUnitContextMenuItem] = []


func _init() -> void:
	var is_test_suite := func is_visible(script: Script, is_ts: bool) -> bool:
		if script == null:
			return false
		return GdUnitTestSuiteScanner.is_test_suite(script) == is_ts
	_context_menus.append(GdUnitContextMenuItem.new(
		GdUnitCommandFileSystemRunTests.ID,
		"Run Testsuites",
		is_test_suite.bind(true)
	))
	_context_menus.append(GdUnitContextMenuItem.new(
		GdUnitCommandFileSystemDebugTests.ID,
		"Debug Testsuites",
		is_test_suite.bind(true)
	))

	# setup shortcuts
	for menu_item in _context_menus:
		if menu_item.shortcut():
			var cb := func call(...files: Array) -> void:
				var paths: Array = files[0]
				menu_item.execute.callv(paths)
			add_menu_shortcut(menu_item.shortcut(), cb)


func _popup_menu(paths: PackedStringArray) -> void:
	for menu_item in _context_menus:
		if menu_item.shortcut():
			add_context_menu_item_from_shortcut(menu_item.name, menu_item.shortcut(), menu_item.icon)
		else:
			add_context_menu_item(menu_item.name, menu_item.execute.bindv(paths).unbind(1), menu_item.icon)
