class_name GdUnitCommand
extends RefCounted


func _init(p_name :String, p_is_enabled: Callable, p_runnable: Callable, p_shortcut :GdUnitShortcut.ShortCut = GdUnitShortcut.ShortCut.NONE) -> void:
	assert(p_name != null, "(%s) missing parameter 'name'" % p_name)
	assert(p_is_enabled != null, "(%s) missing parameter 'is_enabled'" % p_name)
	assert(p_runnable != null, "(%s) missing parameter 'runnable'" % p_name)
	assert(p_shortcut != null, "(%s) missing parameter 'shortcut'" % p_name)
	self.name = p_name
	self.is_enabled = p_is_enabled
	self.shortcut = p_shortcut
	self.runnable = p_runnable


var name: String:
	set(value):
		name = value
	get:
		return name


var shortcut: GdUnitShortcut.ShortCut:
	set(value):
		shortcut = value
	get:
		return shortcut


var is_enabled: Callable:
	set(value):
		is_enabled = value
	get:
		return is_enabled


var runnable: Callable:
	set(value):
		runnable = value
	get:
		return runnable
