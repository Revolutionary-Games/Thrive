@tool
extends ScrollContainer


@onready var _hooks_tree: Tree = %hooks_tree
@onready var _hook_description: RichTextLabel = %hook_description
@onready var _btn_move_up: Button = %hook_actions/btn_move_up
@onready var _btn_move_down: Button = %hook_actions/btn_move_down
@onready var _btn_delete: Button = %hook_actions/btn_delete_hook
@onready var _select_hook_dlg: FileDialog = %select_hook_dlg
@onready var _error_msg_popup :AcceptDialog = %error_msg_popup

var _selected_hook_item: TreeItem = null
var _root: TreeItem
var _system_box_style: StyleBoxFlat
var _priority_box_style: StyleBoxFlat

func _ready() -> void:
	_setup_styles()
	_setup_buttons()
	_setup_tree()
	_load_registered_hooks()


func _setup_styles() -> void:
	_system_box_style = StyleBoxFlat.new()
	_system_box_style.bg_color = Color(1.0, 0.76, 0.03, 1)
	_system_box_style.corner_radius_top_left = 6
	_system_box_style.corner_radius_top_right = 6
	_system_box_style.corner_radius_bottom_left = 6
	_system_box_style.corner_radius_bottom_right = 6
	_priority_box_style = _system_box_style.duplicate()
	_priority_box_style.bg_color = Color(0.26, 0.54, 0.89, 1)


func _setup_buttons() -> void:
	#if Engine.is_editor_hint():
	#	_btn_move_up.icon = GdUnitUiTools.get_icon("MoveUp")
	#	_btn_move_down.icon = GdUnitUiTools.get_icon("MoveDown")
	#	_btn_add.icon = GdUnitUiTools.get_icon("Add")
	#	_btn_delete.icon = GdUnitUiTools.get_icon("Remove")
	pass


func _setup_tree() -> void:
	_hooks_tree.clear()
	_root = _hooks_tree.create_item()
	_hooks_tree.set_columns(2)
	_hooks_tree.set_column_custom_minimum_width(1, 32)
	_hooks_tree.set_column_expand(1, false)
	_hooks_tree.set_hide_root(true)
	_hooks_tree.set_hide_folding(true)
	_hooks_tree.set_select_mode(Tree.SELECT_SINGLE)
	_hooks_tree.item_selected.connect(_on_hook_selected)
	_hooks_tree.item_edited.connect(_on_item_edited)


func _load_registered_hooks() -> void:
	var hook_service := GdUnitTestSessionHookService.instance()
	for hook: GdUnitTestSessionHook in hook_service.enigne_hooks:
		_create_hook_tree_item(hook)

	# Select first item if any
	if _root.get_child_count() > 0:
		var first_item: TreeItem = _root.get_first_child()
		first_item.select(0)
		_on_hook_selected()


func _create_hook_tree_item(hook: GdUnitTestSessionHook) -> TreeItem:
	var item: TreeItem = _hooks_tree.create_item(_root)
	item.set_custom_minimum_height(26)
	# Column 0: Hook info with custom drawing
	item.set_cell_mode(0, TreeItem.CELL_MODE_CUSTOM)
	item.set_custom_draw_callback(0, _draw_hook_item)
	item.set_editable(0, false)
	item.set_metadata(0, hook)
	# Column 1: Checkbox for enable/disable
	item.set_cell_mode(1, TreeItem.CELL_MODE_CHECK)
	item.set_checked(1, GdUnitTestSessionHookService.is_enabled(hook))
	item.set_editable(1, true)
	item.set_custom_bg_color(1, _hook_bg_color(hook))
	item.set_tooltip_text(1, "Enable/Disable the Hook")
	item.propagate_check(1)

	if _is_system_hook(hook):
		item.set_tooltip_text(0, "System hook - (Read-only)")
	else:
		item.set_tooltip_text(0, "User hook")
	return item


func _hook_bg_color(hook: GdUnitTestSessionHook) -> Color:
	if _is_system_hook(hook):
		return Color(0.133, 0.118, 0.090, 1)  # Brownish background for system hooks
	return Color(0.176, 0.196, 0.235, 1)  # Dark background #2d3142


func _draw_hook_item(item: TreeItem, rect: Rect2) -> void:
	var hook := _get_hook(item)
	var is_system := _is_system_hook(hook)
	var is_selected := item == _selected_hook_item

	# Draw background
	var bg_color := _hook_bg_color(hook) # Dark background #2d3142
	if is_selected:
		bg_color = bg_color.lerp(Color(0.2, 0.4, 0.6, 0.3), 0.5)  # Blue tint for selection
	_hooks_tree.draw_rect(rect, bg_color)

	# Draw left border for system hooks
	if is_system:
		var border_rect := Rect2(rect.position.x, rect.position.y, 3, rect.size.y)
		_hooks_tree.draw_rect(border_rect, Color(1.0, 0.76, 0.03, 1))  # Yellow border

	var font := _hooks_tree.get_theme_default_font()

	# Draw hook name
	var hook_name := hook.name
	var text_pos := Vector2(rect.position.x + ( 15 if is_system else 12), rect.position.y + 18)
	var text_color := Color(0.95, 0.95, 0.95, 1)
	_hooks_tree.draw_string(font, text_pos, hook_name, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, text_color)

	# Draw system badge if needed
	if is_system:
		var badge_x := rect.position.x + rect.end.x - 100
		var badge_y := rect.position.y + 14
		var system_badge_rect := Rect2(badge_x, badge_y-8, 48, 16)
		_hooks_tree.draw_style_box(_system_box_style, system_badge_rect)

		var system_text_pos := Vector2(badge_x + 4, badge_y + 4)
		var system_font_size := 10
		_hooks_tree.draw_string(font, system_text_pos, "SYSTEM", HORIZONTAL_ALIGNMENT_CENTER, -1, system_font_size, Color(0.1, 0.1, 0.1, 1))


func _create_hook_display_text(hook_name: String, priority: int, is_system: bool) -> String:
	var text := hook_name + "\n"
	text += "Priority: [color=#4299e1][bgcolor=#4299e1]  " + str(priority) + "  [/bgcolor][/color]"

	if is_system:
		text += " [color=#1a202c][bgcolor=#ffc107]  SYSTEM  [/bgcolor][/color]"

	return text


func _update_hook_description() -> void:
	if _selected_hook_item == null:
		_hook_description.text = "[i]Select a hook to view its description[/i]"
		return
	_hook_description.text = _get_hook(_selected_hook_item).description


func _update_hook_buttons() -> void:
	# Is nothing selected disable the move and delete buttons
	if _selected_hook_item == null:
		_btn_move_up.disabled = true
		_btn_move_down.disabled = true
		_btn_delete.disabled = true
		return

	var hook := _get_hook(_selected_hook_item)
	var is_system := _is_system_hook(hook)

	# Disable the move and delete buttons for system hooks by default
	if is_system:
		_btn_move_up.disabled = true
		_btn_move_down.disabled = true
		_btn_delete.disabled = true
		return

	var prev_item: TreeItem = _selected_hook_item.get_prev()
	var next_item: TreeItem = _selected_hook_item.get_next()

	if prev_item != null:
		var prev_hook := _get_hook(prev_item)
		_btn_move_up.disabled = _is_system_hook(prev_hook)

	_btn_move_down.disabled = next_item == null
	_btn_delete.disabled = false


static func _get_hook(item: TreeItem) -> GdUnitTestSessionHook:
	return item.get_metadata(0)


static func _is_system_hook(hook: GdUnitTestSessionHook) -> bool:
	if hook == null:
		return false
	return hook.get_meta("SYSTEM_HOOK")


func _on_hook_selected() -> void:
	_selected_hook_item = _hooks_tree.get_selected()
	_update_hook_buttons()
	_update_hook_description()


func _on_item_edited() -> void:
	var selected_hook_item := _hooks_tree.get_selected()
	if selected_hook_item != null:
		var hook := _get_hook(selected_hook_item)
		var is_enabled := selected_hook_item.is_checked(1)
		GdUnitTestSessionHookService.instance().enable_hook(hook, is_enabled)


func _on_btn_add_hook_pressed() -> void:
	_select_hook_dlg.show()


func _on_select_hook_dlg_file_selected(path: String) -> void:
	_select_hook_dlg.set_current_path(path)
	_on_select_hook_dlg_confirmed()


func _on_select_hook_dlg_confirmed() -> void:
	_select_hook_dlg.hide()
	var result := GdUnitTestSessionHookService.instance().load_hook(_select_hook_dlg.get_current_path())
	if result.is_error():
		_error_msg_popup.dialog_text = result.error_message()
		_error_msg_popup.show()
		return

	var hook: GdUnitTestSessionHook = result.value()
	result = GdUnitTestSessionHookService.instance().register(hook)
	if result.is_error():
		_error_msg_popup.dialog_text = result.error_message()
		_error_msg_popup.show()
		return

	var hook_added := _create_hook_tree_item(hook)
	_hooks_tree.set_selected(hook_added, 0)


func _on_btn_delete_hook_pressed() -> void:
	if _selected_hook_item != null:
		_root.remove_child(_selected_hook_item)
		GdUnitTestSessionHookService.instance()\
			.unregister(_get_hook(_selected_hook_item))
		_selected_hook_item = null
		_update_hook_buttons()


func _on_btn_move_up_pressed() -> void:
	var prev := _selected_hook_item.get_prev()
	_selected_hook_item.move_before(prev)
	GdUnitTestSessionHookService.instance()\
		.move_before(_get_hook(_selected_hook_item), _get_hook(prev))
	_update_hook_buttons()


func _on_btn_move_down_pressed() -> void:
	var next := _selected_hook_item.get_next()
	_selected_hook_item.move_after(next)
	GdUnitTestSessionHookService.instance()\
		.move_after(_get_hook(_selected_hook_item), _get_hook(next))
	_update_hook_buttons()
