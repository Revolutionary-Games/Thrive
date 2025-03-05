class_name GdUnitShortcutAction
extends RefCounted


func _init(p_type :GdUnitShortcut.ShortCut, p_shortcut :Shortcut, p_command :String) -> void:
	assert(p_type != null, "missing parameter 'type'")
	assert(p_shortcut != null, "missing parameter 'shortcut'")
	assert(p_command != null, "missing parameter 'command'")
	self.type = p_type
	self.shortcut = p_shortcut
	self.command = p_command


var type: GdUnitShortcut.ShortCut:
	set(value):
		type = value
	get:
		return type


var shortcut: Shortcut:
	set(value):
		shortcut = value
	get:
		return shortcut


var command: String:
	set(value):
		command = value
	get:
		return command


func update_shortcut(input_event: InputEventKey) -> void:
	shortcut.set_events([input_event])


func _to_string() -> String:
	return "GdUnitShortcutAction: %s (%s) -> %s" % [GdUnitShortcut.ShortCut.keys()[type], shortcut.get_as_text(), command]
